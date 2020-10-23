using Atoll.Transport;
using MongoDB.Driver;
using System;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    /// <summary>
    /// сервис для возможности резервирования (на основе Lease) бд монги, получения времени сервера бд и прочего функционала
    /// </summary>
    public class MongoDbService : IDbService, IDisposable
    {
        private IMongoDatabase database;
        private IMongoCollection<Lease> leaseCollection;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;
        private readonly ITimeService timeService;
        private readonly ILockService lockService;

        public MongoDbService(
            IUnitIdProvider dhuIdProvider,
            string connString,
            string databaseName,
            TimeSpan leaseCheckTimeout,
            TimeSpan leaseLostTimeout,
            TimeSpan updateServerTimeInterval)
        {
            var client = new MongoClient(connString);

            this.database = client.GetDatabase(databaseName);
            this.leaseCollection = database.GetCollection<Lease>(TransportConstants.LeaseLockTable);
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;
            this.timeService = new MongoDatabaseTimeService(connString, databaseName, updateServerTimeInterval);
            this.lockService = new MongoDbLeaseLockService(dhuIdProvider, this.timeService, connString, databaseName, leaseCheckTimeout, leaseLostTimeout);
        }

        public void Dispose()
        {
            (this.lockService as IDisposable)?.Dispose();
            (this.timeService as IDisposable)?.Dispose();
        }

        public DateTime GetCurrentUtcTime(CancellationToken token)
        {
            return this.timeService.GetCurrentUtcTime(token);
        }

        public ILeaseLockObject CreateLockOrNull(CancellationToken token)
        {
            return this.lockService.CreateLockOrNull(token);
        }

        public bool TryAcquireOrUpdateLease(CancellationToken token)
        {
            return this.lockService.TryAcquireOrUpdateLease(token);
        }
    }

}
