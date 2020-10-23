using Atoll.UtilsBundle.Helpers;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public class MongoDbLeaseLockService : ILockService, IDisposable
    {
        private IMongoDatabase database;
        private IMongoCollection<Lease> leaseCollection;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;

        private readonly object LeaseLockObj = new object();
        private readonly object disposeLock = new object();
        private bool isDisposed;

        private readonly ITimeService timeService;
        private readonly IUnitIdProvider dhuIdProvider;
        private readonly CancellationTokenSource disposeCts;

        public MongoDbLeaseLockService(IUnitIdProvider dhuIdProvider, ITimeService timeService, string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout)
        {
            if ((2 * leaseCheckTimeout.TotalMilliseconds) > leaseLostTimeout.TotalMilliseconds)
            {
                //throw new ArgumentException("lease Check Timeout must be less than lease Lost Timeout", "leaseCheckTimeout");
            }

            var client = new MongoClient(connString);

            this.database = client.GetDatabase(databaseName);
            this.leaseCollection = database.GetCollection<Lease>(TransportConstants.LeaseLockTable);
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;
            this.timeService = timeService;
            this.dhuIdProvider = dhuIdProvider;
            this.disposeCts = new CancellationTokenSource();
        }

        public MongoDbLeaseLockService(IUnitIdProvider dhuIdProvider, string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan updateTimeInterval)
        {
            if ((2 * leaseCheckTimeout.TotalMilliseconds) > leaseLostTimeout.TotalMilliseconds)
            {
                //throw new ArgumentException("lease Check Timeout must be less than lease Lost Timeout", "leaseCheckTimeout");
            }

            var client = new MongoClient(connString);

            this.database = client.GetDatabase(databaseName);
            this.leaseCollection = database.GetCollection<Lease>(TransportConstants.LeaseLockTable);
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;

            this.timeService = new MongoDatabaseTimeService(connString, databaseName, updateTimeInterval);

            this.dhuIdProvider = dhuIdProvider;
        }

        public ILeaseLockObject CreateLockOrNull(CancellationToken token)
        {
            LeaseLockObject leaseLock = null;
            try
            {
                using (var cancelationSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.disposeCts.Token))
                {
                    leaseLock = new LeaseLockObject(this, this.leaseCheckTimeout, this.leaseLostTimeout);
                    if (leaseLock.CheckAndUpdateOrAcquireLease(cancelationSource.Token))
                    {
                        return leaseLock;
                    }
                }                    
            }
            catch (Exception)
            {
            }

            leaseLock?.Dispose();
            return null;
        }

        //private static int retryAcquireLeaseTryCount = 10;
        private bool TryAcquireOrUpdateLeaseInternal(CancellationToken token)
        {
            var dhuId = this.dhuIdProvider.GetId();

            token.ThrowIfCancellationRequested();

            var update = Builders<Lease>.Update
                // нужно указывать что-то одно set или setOnInsert - https://stackoverflow.com/questions/27552352/mongodb-duplicate-fields-in-set-and-setoninsert
                //.SetOnInsert(x => x.ClientId, dhuId)
                .SetOnInsert(x => x.Id, TransportConstants.DbLeaseId)
                // использую CurrentDate
                //.SetOnInsert(x => x.UpdateTime, this.timeService.GetCurrentUtcTime())
                .Set(x => x.ClientId, dhuId)
                .CurrentDate(x => x.UpdateTime);

            var leaseLostTime = this.timeService.GetCurrentUtcTime(token) - this.leaseLostTimeout;

            var getNewLeaseFilter = Builders<Lease>.Filter.Lt(x => x.UpdateTime, leaseLostTime)
                & Builders<Lease>.Filter.Eq(x => x.Id, TransportConstants.DbLeaseId);

            var updateLeaseFilter = Builders<Lease>.Filter.Gte(x => x.UpdateTime, leaseLostTime)
                    & Builders<Lease>.Filter.Eq(x => x.Id, TransportConstants.DbLeaseId)
                    & Builders<Lease>.Filter.Eq(x => x.ClientId, dhuId);

            var filter = getNewLeaseFilter | updateLeaseFilter;

            Lease leaseValue = null;
            var rqStopWatch = Stopwatch.StartNew();
            try
            {
                leaseValue = this.leaseCollection.FindOneAndUpdate(filter, update, new FindOneAndUpdateOptions<Lease, Lease>
                {
                    IsUpsert = true,
                    ReturnDocument = ReturnDocument.After,
                }, token);
            }
            catch(MongoCommandException ex)
            {
                // TODO instrumentation ?
                if (ex.CodeName == "DuplicateKey" || ex.Code == 11000)
                {
                    return false;
                }

                return false;
            }
            catch (MongoWriteException ex)
            {
                // TODO Instrumentation ?

                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                    return false;
                }

                return false;
            }
            finally
            {
                rqStopWatch.Stop();
            }

            // TODO Instrumentation ?
            var requestTimeSpan = rqStopWatch.Elapsed;
            //if (requestTimeSpan > this.leaseCheckTimeout || requestTimeSpan > this.leaseLostTimeout)
            //{
            //    throw new TimeoutException("acquire lease timeout");
            //}

            return leaseValue != null;
        }

        public bool TryAcquireOrUpdateLease(CancellationToken token = default(CancellationToken))
        {
            // не проверяю что уже disposed
            lock (this.LeaseLockObj)
            {
                using (var cancelationSource = CancellationTokenSource.CreateLinkedTokenSource(token, this.disposeCts.Token))
                {
                    // примерно каждый интервал leaseCheckTimeout будет вызываться этот метод, так что считаю что если предыдущий вызов "завис" то мы его отменяем после истечения leaseCheckTimeout
                    // при изменении таймаута следует это учесть в методе "внутреннем" TryAcquireOrUpdateLease
                    //cancelationSource.CancelAfter(this.leaseLostTimeout);
                    cancelationSource.CancelAfter(this.leaseCheckTimeout);

                    try
                    {
                        return this.TryAcquireOrUpdateLeaseInternal(cancelationSource.Token);
                    }
                    //catch (OperationCanceledException)
                    //    when (cancelationSource.IsCancellationRequested)
                    //{
                    //}
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.disposeLock)
            {
                if (this.isDisposed)
                {
                    return;
                }

                this.disposeCts.SafeCancel();
                this.disposeCts.SafeDispose();
                var disposable = this.timeService as IDisposable;
                disposable?.Dispose();
                this.isDisposed = true;
            }

        }
    }

}
