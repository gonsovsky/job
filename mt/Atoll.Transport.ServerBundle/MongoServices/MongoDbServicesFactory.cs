using System;

namespace Atoll.Transport.ServerBundle
{
    public class MongoDbServicesFactory : IDbServicesFactory
    {
        private readonly IUnitIdProvider unitIdProvider;

        public MongoDbServicesFactory(IUnitIdProvider unitIdProvider)
        {
            this.unitIdProvider = unitIdProvider;
        }

        public IDbService GetDbService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null)
        {
            return new MongoDbService(this.unitIdProvider, connString, databaseName, leaseCheckTimeout, leaseLostTimeout, updateTimeInterval ?? TimeSpan.FromMinutes(30));
        }

        public ILockService GetLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null)
        {
            return new MongoDbLeaseLockService(this.unitIdProvider, connString, databaseName, leaseCheckTimeout, leaseLostTimeout, updateTimeInterval ?? TimeSpan.FromMinutes(30));
        }

        public ITimeService GetTimeService(string connString, string databaseName, TimeSpan updateTimeInterval)
        {
            return new MongoDatabaseTimeService(connString, databaseName, updateTimeInterval);
        }
    }

}
