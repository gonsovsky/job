using Atoll.Transport;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication18.Transport
{
    public class MongoDbParameters
    {
        public string MongoConnString { get; set; }
        public string DbName { get; set; }

        public MongoDbParameters(string mongoConnString, string dbName)
        {
            this.MongoConnString = mongoConnString;
            this.DbName = dbName;
        }
    }

    public class SemaphoreLocker
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public async Task LockAsync(Func<Task> worker)
        {
            await this.semaphore.WaitAsync();
            try
            {
                await worker();
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

    public class MongoClusterPacketsStoreSettings
    {
        public TimeSpan? SearchDbTimeout { get; private set; }
        public TimeSpan? LostDbTimeout { get; private set; }

        public MongoClusterPacketsStoreSettings(TimeSpan? searchDbTimeout, TimeSpan? lostDbTimeout)
        {
            this.SearchDbTimeout = searchDbTimeout ?? TimeSpan.FromMilliseconds(300);
            this.LostDbTimeout = lostDbTimeout ?? TimeSpan.FromSeconds(7);
        }
    }

    public static class MongoDbClusterPacketsStoreUtils
    {
        public static DbClusterPacketsStore CreateMongoClusterPacketsStore(DbClusterReservateSettings settings)
        {
            return new DbClusterPacketsStore(new DbClusterLeaseService(new DbClusterUtils(new MongoDbServicesFactory()), settings));
        }
    }

    public class DbClusterPacketsStore : IPacketsStore, IDisposable
    {

        public class CachePair
        {
            public AcquiredDbParameters DbParameters { get; set; }
            public IPacketsStore Store { get; set; }
        }

        private IDbClusterService dbClusterService;
        private CachePair[] cachedPacketsStores;

        private int counter;

        public DbClusterPacketsStore(IDbClusterService dbClusterService)
        {
            this.dbClusterService = dbClusterService;
        }

        private IPacketsStore GetPacketsStore(AcquiredDbParameters acquiredDbParameters, int dbCount)
        {
            // no-lock вариант...
            var oldCachedStores = this.cachedPacketsStores;
            var storeKeyPair = oldCachedStores.FirstOrDefault(x => x.DbParameters.Equals(acquiredDbParameters));
            if (storeKeyPair != null)
            {
                return storeKeyPair.Store;
            }
            else
            {
                var newStore = new MongoDbPacketsStore(new MongoDbParameters(acquiredDbParameters.ConnStringOrUrl, acquiredDbParameters.DatabaseName));
                var newCachedStores = (new CachePair[] { new CachePair
                        {
                            DbParameters = acquiredDbParameters,
                            Store = newStore
                        }
                    })
                    // последние 3 стора
                    .Union(oldCachedStores.Take(dbCount - 1))
                    .ToArray();

                this.cachedPacketsStores = newCachedStores;

                return newStore;
            }
        }

        private IPacketsStore GetPacketsStoreRoundRobin(AcquiredLockServices[] services)
        {
            if (services == null || services.Length == 0)
            {
                throw new InvalidOperationException("no reserved store for packets");
            }

            // "аналог" round-robin (если количество зарезервированных бд не скачет, то будет round-robin)
            var counter = unchecked((uint)Interlocked.Increment(ref this.counter));
            var current = counter % services.Length;
            var service = services[current];
            return this.GetPacketsStore(service.AcquiredDbParameters, services.Length);
        }

        public async Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentifierData agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leaseResult = await this.dbClusterService.RenewLeaseAsync(cancellationToken);
            var store = this.GetPacketsStoreRoundRobin(leaseResult.ReservedList);
            return await store.AddIfNotExistsPacketPartAsync(agentId, request, cancellationToken);
        }

        public async Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentifierData agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leaseResult = await this.dbClusterService.RenewLeaseAsync(cancellationToken);
            var store = this.GetPacketsStoreRoundRobin(leaseResult.ReservedList);
            return await store.AddIfNotExistsPacketsPartsAsync(agentId, requests, cancellationToken);
        }

        public void Dispose()
        {
            this.dbClusterService?.Dispose();
        }

        public async Task<bool> CheckIfHasReservationsAsync(CancellationToken token)
        {
            return await this.dbClusterService.CheckIfHasLeaseAsync(token);
        }

        public async Task<bool> PingAsync(CancellationToken token)
        {
            try
            {
                var leaseResult = await this.dbClusterService.RenewLeaseAsync(token);
                if (leaseResult.ReservedList != null && leaseResult.ReservedList.Any())
                {
                    // TODO требуется возвращать более подробную информацию! Чтобы было видно что часть бд перестала отвечать...
                    var stores = this.GetPacketsStoreRoundRobin(leaseResult.ReservedList);
                    foreach (var item in leaseResult.ReservedList)
                    {
                        try
                        {
                            var store = this.GetPacketsStore(item.AcquiredDbParameters, leaseResult.ReservedList.Length);
                            var isAlive = await store.PingAsync(token);
                            if (isAlive)
                            {
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

    public class MongoDbPacketsStore : IPacketsStore
    {
        private readonly MongoDbParameters mongoDbParameters;
        private readonly IMongoDatabase database;
        private readonly IMongoCollection<PacketPart> chunks;
        private readonly SemaphoreLocker initLocker = new SemaphoreLocker();
        private bool initialized = false;
        private string storageToken;

        public MongoDbPacketsStore(MongoDbParameters mongoDbParameters)
        {
            this.mongoDbParameters = mongoDbParameters;

            // Create client connection to our MongoDB database
            var client = new MongoClient(this.mongoDbParameters.MongoConnString);
            this.database = client.GetDatabase(this.mongoDbParameters.DbName);
            this.chunks = database.GetCollection<PacketPart>(TransportConstants.PacketsPartsTable);
        }

        protected async Task<string> GetOrCreateStorageToken(CancellationToken cancellationToken = default(CancellationToken))
        {
            var storageTokenCollection = database.GetCollection<GlobalSetting>(TransportConstants.GlobalSettingTable);

            string storageToken = null;
            var tryCount = 5;
            Exception lastEx = null;
            for (int i = 0; i < tryCount; i++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var filter = Builders<GlobalSetting>.Filter.Eq(x => x.Id, TransportConstants.StorageTokenKey);
                    var token = await (await storageTokenCollection.FindAsync<GlobalSetting>(filter, null, cancellationToken)).FirstOrDefaultAsync(cancellationToken);
                    if (token == null)
                    {
                        token = new GlobalSetting
                        {
                            // 
                            Id = TransportConstants.StorageTokenKey,
                            Token = Guid.NewGuid().ToString(),
                        };

                        storageTokenCollection.InsertOne(token, null, cancellationToken);
                    }

                    storageToken = token.Token.ToString();

                    lastEx = null;
                    break;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                }
            }

            if (lastEx != null)
            {
                throw lastEx;
            }

            return storageToken;
        }

        private async Task InitIndexes(CancellationToken cancellationToken)
        {
            // чтобы не было дублей
            var indexKeys = Builders<PacketPart>.IndexKeys.Ascending(x => x.PacketId).Ascending(x => x.StartPosition);
            var ixName = "uix_packetid_startpos";
            var ixModel = new CreateIndexModel<PacketPart>(indexKeys, new CreateIndexOptions
            {
                Name = ixName,
                Unique = true,
            });

            await this.chunks.Indexes.CreateOneAsync(ixModel, null, cancellationToken).ConfigureAwait(false);
        }

        //private async Task CreateIndexWithDropRetry()
        //{
        //    try
        //    {
        //        await this.chunks.Indexes.CreateOneAsync(ixModel, null, cancellationToken).ConfigureAwait(false);
        //    }
        //    catch (MongoWriteConcernException ex)
        //    {
        //        await this.chunks.Indexes.DropOneAsync(ixName, cancellationToken).ConfigureAwait(false);
        //        await this.chunks.Indexes.CreateOneAsync(new CreateIndexModel<PacketPart>(indexKeys), null, cancellationToken).ConfigureAwait(false);
        //    }
        //}

        protected async Task InitInternalAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            //await this.LockMongoDb(cancellationToken);
            await this.InitIndexes(cancellationToken);
            this.storageToken = await GetOrCreateStorageToken(cancellationToken);
        }

        private async Task UpdateLeaseIfNeeded(CancellationToken cancellationToken = default(CancellationToken))
        {

        }

        public async Task InitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.initialized)
            {
                return;
            }

            await initLocker.LockAsync(async () =>
            {
                if (this.initialized)
                {
                    return;
                }

                await this.InitInternalAsync(cancellationToken);

                this.initialized = true;
            });
        }

        public async Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentifierData agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentException("request");
            }

            if (!this.initialized)
            {
                await this.InitAsync(cancellationToken);
            }

            try
            {
                await this.UpdateLeaseIfNeeded();
                await this.chunks.InsertOneAsync(this.Get(request));
                return AddAddPacketPartResult.SuccessResult(request);
            }
            catch (MongoWriteException ex)
            {
                // Instrumentation ???
                var nonDupKeyExceptions = ex.WriteError.Category != ServerErrorCategory.DuplicateKey;
                if (nonDupKeyExceptions)
                {
                    return AddAddPacketPartResult.FailResult(request);
                }

                return AddAddPacketPartResult.SuccessResult(request);
            }
        }

        public async Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentifierData agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (requests == null)
            {
                throw new ArgumentException("requests");
            }

            if (!requests.Any())
            {
                return AddAddPacketsPartsResult.EmptyResult();
            }

            if (!this.initialized)
            {
                await this.InitAsync(cancellationToken);
            }

            // IsOrdered = false, неупорядоченные операции - при выполнении отдельных операций они выполняются не упорядочено и они не останавливают исполнение остальных операций
            try
            {
                await this.chunks.InsertManyAsync(requests.Select(this.Get), new InsertManyOptions() { IsOrdered = false });
                return AddAddPacketsPartsResult.CreateResult(this.storageToken, requests.Select(request => AddAddPacketPartResult.SuccessResult(request)));
            }
            catch (MongoBulkWriteOperationException ex)
            {
                // Instrumentation ???

                var results = new List<AddAddPacketPartResult>();
                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    var rqNonDupKeyErrors = ex.WriteErrors.Where(x => x.Index == i && x.Category != ServerErrorCategory.DuplicateKey);
                    results.Add(rqNonDupKeyErrors.Any() ? AddAddPacketPartResult.FailResult(request) : AddAddPacketPartResult.SuccessResult(request));              
                }

                return AddAddPacketsPartsResult.CreateResult(this.storageToken, results);
            }
        }

        private PacketPart Get(AddPacketPartRequest request)
        {
            return new PacketPart
            {
                ProviderKey = request.ProviderKey,
                PacketId = request.PacketId,
                IsFinal = request.IsFinal,
                StartPosition = request.StartPosition,
                EndPosition = request.EndPosition,
                Bytes = request.Bytes,
                FinalPartTransferTime = request.IsFinal ? (DateTime?)DateTime.UtcNow : null,
            };
        }

        public Task<bool> CheckIfHasReservationsAsync(CancellationToken token)
        {
            return Task.FromResult(true);
        }

        public Task<bool> PingAsync(CancellationToken token)
        {
            return Task.FromResult(this.database.Ping(token));
        }
    }

    public static class MongoExtensions
    {
        public static bool Ping(this IMongoDatabase database, CancellationToken token)
        {
            //https://stackoverflow.com/questions/28835833/how-to-check-connection-to-mongodb
            //https://stackoverflow.com/questions/30713599/mongodb-driver-2-0-c-sharp-is-there-a-way-to-find-out-if-the-server-is-down-in
            try
            {
                bool isMongoLive = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", null, token).Wait(1000, token);
                return isMongoLive;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

}
