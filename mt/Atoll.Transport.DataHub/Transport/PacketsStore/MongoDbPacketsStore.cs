using Atoll.Transport;
using Atoll.Transport.ServerBundle;
using MongoDB.Driver;
using MongoDB.Driver.Core.Bindings;
using MongoDB.Driver.Core.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataHub
{

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
            
            // Cappped ??
            //CreateCollectionOptions options = new CreateCollectionOptions()
            //{
            //};
            //this.database.CreateCollection(TransportConstants.PacketsPartsTable, options);
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
            await new MongoIndexCreator().InitIndexes(this.database, cancellationToken);
        }

        public async Task InitInternalAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await this.InitIndexes(cancellationToken);
            this.storageToken = await GetOrCreateStorageToken(cancellationToken);
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

        public async Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentity agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentException("request");
            }

            if (!this.initialized)
            {
                await this.InitAsync(cancellationToken);
            }

            var doc = this.Get(agentId, request);
            try
            {
                await this.chunks.InsertOneAsync(doc, null, cancellationToken);
                return AddAddPacketPartResult.SuccessResult(request, this.storageToken, doc.Id);
            }
            catch (MongoWriteException ex)
            {
                // Instrumentation ???
                var nonDupKeyExceptions = ex.WriteError.Category != ServerErrorCategory.DuplicateKey;
                if (nonDupKeyExceptions)
                {
                    return AddAddPacketPartResult.FailResult(request);
                }

                // пока не удастся получить id для документа который зафейлился из exception-а
                // https://jira.mongodb.org/browse/SERVER-4637
                var id = await this.chunks.Find(x => x.PacketId == doc.PacketId && x.StartPosition == doc.StartPosition).Project(x => x.Id).FirstOrDefaultAsync(cancellationToken);
                if (id == null)
                {
                    return AddAddPacketPartResult.FailResult(request);
                }
                return AddAddPacketPartResult.SuccessResult(request, this.storageToken, id);
            }
        }

        public async Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentity agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken))
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

            var stopWatch = Stopwatch.StartNew();
            // IsOrdered = false, неупорядоченные операции - при выполнении отдельных операций они выполняются не упорядочено и они не останавливают исполнение остальных операций
            try
            {
                var docs = requests.Select(x => this.Get(agentId, x)).ToList();
                await this.chunks.InsertManyAsync(docs, new InsertManyOptions() { IsOrdered = false }, cancellationToken);
                stopWatch.Stop();
                return AddAddPacketsPartsResult.CreateResult(this.storageToken, requests.Select((request, i) => {
                    return AddAddPacketPartResult.SuccessResult(request, this.storageToken, docs[i].Id);
                }));
            }
            catch (MongoBulkWriteOperationException ex)
            {
                // Instrumentation ???
                var results = new List<AddAddPacketPartResult>();
                for (int i = 0; i < requests.Count; i++)
                {
                    var request = requests[i];
                    var rqNonDupKeyErrors = ex.WriteErrors.Where(x => x.Index == i && x.Category != ServerErrorCategory.DuplicateKey);
                    if (rqNonDupKeyErrors.Any())
                    {
                        results.Add(AddAddPacketPartResult.FailResult(request));
                    }
                    else
                    {
                        // TODO оптимизировать, не делать запрос на каждый fail вариант
                        var id = await this.chunks.Find(x => x.PacketId == request.PacketId && x.StartPosition == request.StartPosition).Project(x => x.Id).FirstOrDefaultAsync(cancellationToken);
                        if (id == null)
                        {
                            results.Add(AddAddPacketPartResult.FailResult(request));
                        }
                        else
                        {
                            results.Add(AddAddPacketPartResult.SuccessResult(request, this.storageToken, id));
                        }
                    }
                }

                //var failed = new List<AddAddPacketPartResult>();
                //var reqToSelect = new List<AddPacketPartRequest>();
                //for (int i = 0; i < requests.Count; i++)
                //{
                //    var request = requests[i];
                //    var rqNonDupKeyErrors = ex.WriteErrors.Where(x => x.Index == i && x.Category != ServerErrorCategory.DuplicateKey);
                //    if (rqNonDupKeyErrors.Any())
                //    {
                //        failed.Add(AddAddPacketPartResult.FailResult(request));
                //    }
                //    else
                //    {
                //        reqToSelect.Add(request);
                //    }
                //}

                //// select
                //Expression<Func<PacketPart, bool>> filter = null;

                //var f = Builders<PacketPart>.Filter
                //    .Or(reqToSelect.Select(r =>))

                //FilterDefinition<PacketPart> filterDef = 
                //var ids = await this.chunks.Find(x => x.PacketId == request.PacketId && x.StartPosition == request.StartPosition).Project(x => x.Id).FirstOrDefaultAsync(cancellationToken);

                stopWatch.Stop();
                return AddAddPacketsPartsResult.CreateResult(this.storageToken, results);
            }
        }

        private PacketPart Get(AgentIdentity agentId, AddPacketPartRequest request)
        {
            var now = DateTime.UtcNow;
            return new PacketPart
            {
                ProviderKey = request.ProviderKey,
                PacketId = request.PacketId,
                StartPosition = request.StartPosition,
                EndPosition = request.EndPosition,
                Bytes = request.Bytes,
                // TODO надо бы переделать на время сервера БД
                CreatedTime = now,
                FinalPartTransferTime = request.IsFinal ? now : (DateTime?)null,
                AgentInfo = new ServerBundle.AgentIdentity
                {
                    ComputerName = agentId.ComputerName,
                    DomainName = agentId.DomainName,
                },
                PreviousPartStorageToken = request.PreviousPartStorageToken,
                PreviousPartId = request.PreviousPartId,
            };
        }

        public bool CheckIfHasReservations()
        {
            return true;
        }

        public Task<bool> PingAsync(CancellationToken token)
        {
            return Task.FromResult(this.database.Ping(token, 2* 1000));
        }
    }

}
