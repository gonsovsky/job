using Atoll.Transport;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebApplication18.Transport
{
    public interface IDbService : ITimeService, ILockService
    {
    }

    public interface ITimeService : IDisposable
    {
        DateTime GetCurrentUtcTime();
    }

    public interface ILeaseLockObject : IDisposable
    {
        bool CheckLease();
        bool CheckAndUpdateLease();
        bool CheckAndUpdateOrAcquireLease(CancellationToken token);
        CancellationToken GetLoseLeaseToken();
    }

    public interface ILockService
    {
        ILeaseLockObject CreateLockOrNull(CancellationToken token = default(CancellationToken));

        bool TryAcquireOrUpdateLease(CancellationToken token = default(CancellationToken));
    }

    // с исполнением eval не стал разбираться - Command eval failed: no such command: 'eval'.
    //public static class MongoClientExtensions
    //{
    //    /// <summary>
    //    /// Evaluates the specified javascript within a MongoDb database
    //    /// </summary>
    //    /// <param name="database">MongoDb Database to execute the javascript</param>
    //    /// <param name="javascript">Javascript to execute</param>
    //    /// <returns>A BsonValue result</returns>
    //    public static async Task<BsonValue> EvalAsync(this IMongoDatabase database, string javascript)
    //    {
    //        var client = database.Client as MongoClient;

    //        if (client == null)
    //            throw new ArgumentException("Client is not a MongoClient");

    //        var function = new BsonJavaScript(javascript);
    //        var op = new EvalOperation(database.DatabaseNamespace, function, null);

    //        using (var writeBinding = new WritableServerBinding(client.Cluster, new CoreSessionHandle(NoCoreSession.Instance)))
    //        {
    //            return await op.ExecuteAsync(writeBinding, CancellationToken.None);
    //        }
    //    }
    //}

    public interface IDbServicesFactory
    {
        ITimeService GetTimeService(string connString, string databaseName, TimeSpan updateTimeInterval);
        //ILockService GetLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null);
        IDbService GetDbService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null);
    }

    public class MongoDbServicesFactory : IDbServicesFactory
    {
        public IDbService GetDbService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null)
        {
            return new MongoDbService(connString, databaseName, leaseCheckTimeout, leaseLostTimeout, updateTimeInterval ?? TimeSpan.FromMinutes(30));
        }

        public ILockService GetLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null)
        {
            return new MongoDbLeaseLockService(connString, databaseName, leaseCheckTimeout, leaseLostTimeout, updateTimeInterval ?? TimeSpan.FromMinutes(30));
        }

        public ITimeService GetTimeService(string connString, string databaseName, TimeSpan updateTimeInterval)
        {
            return new MongoDatabaseTimeService(connString, databaseName, updateTimeInterval);
        }
    }

    /// <summary>
    /// Сервис получения времени (DateTime) сервера Mongodb
    /// </summary>
    /// <remarks>
    /// на данный момент в Mongo отсутствует оператор текущего timestamp/datetime который может работать в запросах или который можно вернуть в результате (есть $currentDate для Update и $$NOW для aggregation pipeline)
    /// </remarks>
    public class MongoDatabaseTimeService : ITimeService, IDisposable
    {
        private IMongoDatabase database;
        // для полуцчения текущей даты сервера mongodb
        private readonly TimeSpan updateTimeInterval;
        private Stopwatch stopwatch;
        private DateTime? etalonTime;
        private Timer updateDatetimeTimer;
        private bool isDisposed;

        // lock-и
        private readonly object DatesLockObj = new object();

        public MongoDatabaseTimeService(string connString, string databaseName, TimeSpan updateTimeInterval/*, bool startUpdateTimer*/)
        {
            var client = new MongoClient(connString);
            this.database = client.GetDatabase(databaseName);
            this.updateTimeInterval = updateTimeInterval;

            //if (startUpdateTimer)
            //{
            //    this.UpdateTime();
            //    this.StartTimerIfNotStarted();
            //}
        }

        private void StartTimerIfNotStarted()
        {
            if (this.updateDatetimeTimer == null)
            {
                lock (this.DatesLockObj)
                {
                    if (this.updateDatetimeTimer == null && !this.isDisposed)
                    {
                        this.updateDatetimeTimer = TimerUtils.CreateNonOverlapped(this.UpdateTimerCallBack, null, this.updateTimeInterval, this.updateTimeInterval);
                    }
                }
            }

        }

        public void Dispose()
        {
            lock (this.DatesLockObj)
            {
                this.stopwatch?.Stop();

                this.updateDatetimeTimer?.Dispose();
                // UpdateTime завязан на null
                this.updateDatetimeTimer = null;

                this.isDisposed = true;
            }
        }

        private static TimeSpan maxAllowedDelta = TimeSpan.FromSeconds(1);

        private void UpdateTimerCallBack(object state)
        {
            this.UpdateTime();
        }

        private void UpdateTime()
        {
            if (this.isDisposed)
            {
                return;
            }

            try
            {
                // по сути дате которая вернулась с сервера(dbReturnedTime) соответствует какая-то из дат в промежутке (timeBeforeRequest, timeAfterRequest)
                // если просто "выбрать медиану", т.е. для dbReturnedTime соответствует дата timeBeforeRequest + ((timeAfterRequest - timeBeforeRequest)/2)
                // можем допустить что dbReturnedTime + ((timeAfterRequest - timeBeforeRequest)/2) + stopWatch.elapsed будет определать текущее время на сервере бд
                // также будет хорошо оценить насколько велика разница timeAfterRequest - timeBeforeRequest, по сути она должна буть очень незначительна, может при достижении определённой величины, следует делать дополнительные попытки
                // также получать дату с сервера лучше когда нагрузка не слишком большая (поэтому дату мы можем обновлять вместе с lease lock-ом)
                var requestStopWatch = Stopwatch.StartNew();
                var dbReturnedTime = this.GetCurrentUtcFromMongoServer();
                var newStopWatch = Stopwatch.StartNew();
                requestStopWatch.Stop();
                var requestTimeSpan = requestStopWatch.Elapsed;
                if (requestTimeSpan > maxAllowedDelta)
                {
                    // TODO instrumentation ? retry ? Throw ?
                }

                var dbTime = dbReturnedTime + (requestTimeSpan / 2);

                lock (this.DatesLockObj)
                {
                    var isDisposed = this.isDisposed;

                    var oldStopwatch = this.stopwatch;
                    oldStopwatch?.Stop();

                    if (!isDisposed)
                    {
                        this.stopwatch = newStopWatch;
                        this.etalonTime = dbTime;
                    }
                    else
                    {
                        newStopWatch.Stop();
                    }
                }
            }
            catch (Exception)
            {
                // TODO instrumentation ?
                throw;
            }
        }

        private static BsonDocumentCommand<BsonDocument> hostInfoCmd = new BsonDocumentCommand<BsonDocument>(new BsonDocument { { "hostInfo", 1 } });
        private DateTime GetCurrentUtcFromMongoServer()
        {
            // на данный момент в Mongo отсутствует оператор текущего timestamp/datetime который может работать в запросах или который можно вернуть в результате (есть $currentDate для Update и $$NOW для aggregation pipeline)
            // https://docs.mongodb.com/manual/reference/command/hostInfo/
            // hostInfo.system.currentTime. A timestamp of the current system time.
            // также возможно использовать serverStatus, но hostInfo быстрее работает
            // с исполнением eval не стал разбираться - Command eval failed: no such command: 'eval'.
            // var result = this.database.EvalAsync("return new Date()").Result;
            var cmdResult = this.database.RunCommand(hostInfoCmd);
            var result = cmdResult["system"]["currentTime"].ToUniversalTime();
            return result;
        }

        public DateTime GetCurrentUtcTime()
        {
            lock (this.DatesLockObj)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("TimeService");
                }

                if (this.etalonTime == null)
                {
                    this.UpdateTime();
                    this.StartTimerIfNotStarted();
                }

                return this.etalonTime.Value + this.stopwatch.Elapsed;
            }
        }
    }

    public class LeaseLockObject : ILeaseLockObject
    {
        private DateTime leaseUpdateTime;
        private bool hasLease;
        private readonly ILockService lockService;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;
        private Timer timer;
        private bool leaseFirstCheckOccured = false;
        //private bool isDisposed = false;
        // пока без локов
        private readonly object sync = new object();
        private bool isDisposed;
        //private object

        public LeaseLockObject(ILockService lockService, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout)
        {
            this.lockService = lockService;
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;
            this.timer = new Timer(this.UpdateLeaseTimeCallback, null, leaseCheckTimeout, leaseCheckTimeout);
        }

        private void UpdateLeaseTimeCallback(object state)
        {
            var newPeriod = this.leaseCheckTimeout;
            if (DateTime.UtcNow > this.leaseUpdateTime + this.leaseCheckTimeout)
            {
                // пока не требуется обновлять, но момент в который потребуется обновить поменялся (т.к. в CheckAndUpdateLease есть обновление)
                newPeriod = this.leaseUpdateTime + this.leaseCheckTimeout - DateTime.UtcNow;
            }
            else
            {
                this.UpdateLease();
            }

            try
            {
                this.timer?.Change(newPeriod, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }

            this.leaseFirstCheckOccured = true;
        }

        private static int tryAcquireLeaseRetryCount = 4;

        private void UpdateLease(CancellationToken token = default(CancellationToken))
        {
            bool hasLease;
            try
            {
                hasLease = this.lockService.TryAcquireOrUpdateLease(token);
            }
            catch (OperationCanceledException)
            {
                hasLease = false;
            }
            catch (Exception)
            {
                hasLease = false;

                for (int i = 0; i < tryAcquireLeaseRetryCount; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    try
                    {
                        hasLease = this.lockService.TryAcquireOrUpdateLease(token);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        hasLease = false;
                        break;
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            this.hasLease = hasLease;
            this.leaseUpdateTime = DateTime.UtcNow;

            this.leaseFirstCheckOccured = true;

            // лиза потеряна
            if (!this.hasLease)
            {
                List<CancellationTokenSource> loseLeaseCtsListLocal = null;
                lock (this.sync)
                {
                    if (!this.isDisposed)
                    {
                        loseLeaseCtsListLocal = this.loseLeaseCtsList;
                        this.loseLeaseCtsList = new List<CancellationTokenSource>();
                    }
                }

                if (loseLeaseCtsListLocal != null)
                {
                    foreach (var loseLeaseCts in loseLeaseCtsListLocal)
                    {
                        loseLeaseCts.Cancel();
                    }
                }
            }
        }

        public bool CheckAndUpdateLease()
        {
            // данные до истечения таймаута проверки lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseCheckTimeout)
            {
                return this.hasLease;
            }

            // данные до истечения таймаута жизни lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseLostTimeout)
            {
                if (!this.hasLease)
                {
                    return false;
                }
                else
                {
                    this.UpdateLease();
                    return this.hasLease;
                }
            }
            else
            {
                // после истечения
                //if (!this.leaseFirstCheckOccured)
                //{
                //    this.UpdateLease();
                //    return this.hasLease;
                //}

                return false;
            }
        }

        public bool CheckAndUpdateOrAcquireLease(CancellationToken token)
        {
            // данные до истечения таймаута проверки lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseCheckTimeout)
            {
                return this.hasLease;
            }

            // данные до истечения таймаута жизни lease, но уже после leaseCheckTimeout
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseLostTimeout)
            {
                if (!this.hasLease)
                {
                    return false;
                }
                else
                {
                    this.timer?.Change(50, Timeout.Infinite);
                    return this.hasLease;
                }
            }
            else
            {
                // после истечения
                this.UpdateLease(token);
                return this.hasLease;
            }
        }

        public void Dispose()
        {
            lock (this.sync)
            {
                this.timer?.Dispose();
                this.timer = null;

                var loseLeaseCtsListLocal = this.loseLeaseCtsList;
                this.loseLeaseCtsList = null;
                if (loseLeaseCtsListLocal != null)
                {
                    foreach (var loseLeaseTcs in loseLeaseCtsListLocal)
                    {
                        loseLeaseTcs.Dispose();
                    }
                }
            }
        }

        private List<CancellationTokenSource> loseLeaseCtsList = new List<CancellationTokenSource>();
        public CancellationToken GetLoseLeaseToken()
        {
            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("lease lock");
                }

                var cts = new CancellationTokenSource();
                this.loseLeaseCtsList.Add(cts);
                if (!this.hasLease)
                {
                    cts.Cancel();
                }
                return cts.Token;
            }
        }

        public bool CheckLease()
        {
            return this.hasLease;
        }
    }

    public class MongoDbLeaseLockService : ILockService, IDisposable
    {
        private IMongoDatabase database;
        private IMongoCollection<Lease> leaseCollection;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;

        private readonly object LeaseLockObj = new object();

        private readonly ITimeService timeService;
        private readonly IUnitIdProvider dhuIdProvider;

        public MongoDbLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, ITimeService timeService)
        {
            if ((2 * leaseCheckTimeout) > leaseLostTimeout)
            {
                //throw new ArgumentException("lease Check Timeout must be less than lease Lost Timeout", "leaseCheckTimeout");
            }

            var client = new MongoClient(connString);

            this.database = client.GetDatabase(databaseName);
            this.leaseCollection = database.GetCollection<Lease>(TransportConstants.LeaseLockTable);
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;
            this.timeService = timeService;
        }

        public MongoDbLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan updateTimeInterval)
        {
            if ((2 * leaseCheckTimeout) > leaseLostTimeout)
            {
                //throw new ArgumentException("lease Check Timeout must be less than lease Lost Timeout", "leaseCheckTimeout");
            }

            var client = new MongoClient(connString);

            this.database = client.GetDatabase(databaseName);
            this.leaseCollection = database.GetCollection<Lease>(TransportConstants.LeaseLockTable);
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;

            this.timeService = new MongoDatabaseTimeService(connString, databaseName, updateTimeInterval);
        }

        public ILeaseLockObject CreateLockOrNull(CancellationToken token)
        {
            LeaseLockObject leaseLock = null;
            try
            {
                leaseLock = new LeaseLockObject(this, this.leaseCheckTimeout, this.leaseLostTimeout);
                if (leaseLock.CheckAndUpdateOrAcquireLease(token))
                {
                    return leaseLock;
                }
            }
            catch (Exception)
            {
            }

            leaseLock?.Dispose();
            return null;
        }

        private bool TryAcquireOrUpdateLease(int tryIndex, CancellationToken token)
        {
            var dhuId = this.dhuIdProvider.GetId();

            token.ThrowIfCancellationRequested();

            var update = Builders<Lease>.Update
                .SetOnInsert(x => x.ClientId, dhuId)
                .SetOnInsert(x => x.Id, TransportConstants.DbLeaseId)
                //.SetOnInsert(x => x.UpdateTime, this.timeService.GetCurrentUtcTime())
                .Set(x => x.ClientId, dhuId)
                .CurrentDate(x => x.UpdateTime);

            var leaseLostTime = this.timeService.GetCurrentUtcTime() - this.leaseLostTimeout;

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

            var requestTimeSpan = rqStopWatch.Elapsed;
            if (requestTimeSpan > this.leaseCheckTimeout || requestTimeSpan > this.leaseLostTimeout)
            {
                // TODO Instrumentation ?
                // маловероятный исход
                if (tryIndex >= 10)
                {
                    //return false;
                    throw new TimeoutException("acquire lease timeout");
                }

                return this.TryAcquireOrUpdateLease(tryIndex++, token);
            }

            return leaseValue != null;
        }

        public bool TryAcquireOrUpdateLease(CancellationToken token = default(CancellationToken))
        {
            // не проверяю что уже disposed
            lock (this.LeaseLockObj)
            {
                using (var cancelationSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cancelationSource.CancelAfter(this.leaseLostTimeout);

                    try
                    {
                        return this.TryAcquireOrUpdateLease(0, cancelationSource.Token);
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
            var disposable = this.timeService as IDisposable;
            disposable?.Dispose();
        }
    }

    public class AcquiredLockServices : IDisposable
    {
        public AcquiredDbParameters AcquiredDbParameters { get; }
        public ILockService LockService { get; }
        public ITimeService TimeService { get; }
        public ILeaseLockObject LockObject { get; }

        public AcquiredLockServices(AcquiredDbParameters acquiredDbParameters, ILockService lockService, ITimeService timeService, ILeaseLockObject lockObject)
        {
            this.AcquiredDbParameters = acquiredDbParameters;
            this.LockService = lockService;
            this.TimeService = timeService;
            this.LockObject = lockObject;
        }

        public void Dispose()
        {
            (this.LockService as IDisposable)?.Dispose();
            (this.TimeService as IDisposable)?.Dispose();
            (this.LockObject as IDisposable)?.Dispose();
        }
    }

    public class AcquiredDbParameters : IEquatable<AcquiredDbParameters>
    {
        public string ConnStringOrUrl { get; set; }
        public string DatabaseName { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as AcquiredDbParameters);
        }

        public bool Equals(AcquiredDbParameters other)
        {
            return other != null &&
                   ConnStringOrUrl == other.ConnStringOrUrl &&
                   DatabaseName == other.DatabaseName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ConnStringOrUrl, DatabaseName);
        }

        //public TimeSpan LeaseCheckTimeout { get; set; }
        //public TimeSpan LeaseLostTimeout { get; set; }
        //public TimeSpan UpdateServerTimeInterval { get; set; }
    }

    public class DbClusterParameters
    {
        public IList<string> ConnStringsOrUrls { get; set; }
        public string DatabaseName { get; set; }
        public TimeSpan LeaseCheckTimeout { get; set; }
        public TimeSpan LeaseLostTimeout { get; set; }
        public TimeSpan UpdateServerTimeInterval { get; set; }
    }

    /// <summary>
    /// Сервис для получения лока
    /// </summary>
    public interface IDbClusterUtils
    {
        AcquiredLockServices AcquireServicesForAnyUrlsOrDefault(IList<string> connStringsOrUrls,
            string databaseName,
            TimeSpan leaseCheckTimeout,
            TimeSpan leaseLostTimeout,
            TimeSpan updateServerTimeInterval,
            CancellationToken token);

        AcquiredLockServices AcquireServicesForAnyUrlsOrDefault(DbClusterParameters parameters, CancellationToken token);
    }

    public class DbClusterUtils : IDbClusterUtils
    {
        private readonly IDbServicesFactory dbServicesFactory;

        public DbClusterUtils(IDbServicesFactory dbServicesFactory)
        {
            this.dbServicesFactory = dbServicesFactory;
        }

        public AcquiredLockServices AcquireServicesForAnyUrlsOrDefault(IList<string> urls,
            string databaseName,
            TimeSpan leaseCheckTimeout,
            TimeSpan leaseLostTimeout,
            TimeSpan updateServerTimeInterval,
            CancellationToken token)
        {
            foreach (var url in urls)
            {
                IDbService dbService = null;
                try
                {
                    dbService = this.dbServicesFactory.GetDbService(url, databaseName, leaseCheckTimeout, leaseLostTimeout, updateServerTimeInterval);
                    //mongoDbService = new MongoDbService(url, databaseName, leaseCheckTimeout, leaseLostTimeout, updateServerTimeInterval);

                    ILeaseLockObject leaseLock = null;
                    try
                    {
                        leaseLock = dbService.CreateLockOrNull(token);
                        if (leaseLock != null)
                        {
                            return new AcquiredLockServices(new AcquiredDbParameters
                            {
                                ConnStringOrUrl = url,
                                DatabaseName = databaseName,
                            }, dbService, dbService, leaseLock);
                        }
                        else
                        {
                            dbService.Dispose();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // отмена
                        return null;
                    }
                    catch
                    {
                        leaseLock?.Dispose();
                        throw;
                    }
                }
                catch (OperationCanceledException)
                {
                    // отмена
                    return null;
                }
                catch
                {
                    dbService?.Dispose();
                    continue;
                }
            }

            return null;
        }

        public AcquiredLockServices AcquireServicesForAnyUrlsOrDefault(DbClusterParameters parameters, CancellationToken token)
        {
            return this.AcquireServicesForAnyUrlsOrDefault(parameters.ConnStringsOrUrls, parameters.DatabaseName, parameters.LeaseCheckTimeout, parameters.LeaseLostTimeout, parameters.UpdateServerTimeInterval, token);
        }
    }

    public class ReserveResult
    {
        public bool IsSuccess { get; set; }
        public AcquiredLockServices[] ReservedList { get; set; }

        public static ReserveResult SuccessResult(params AcquiredLockServices[] acquiredLockServices)
        {
            if (acquiredLockServices == null)
            {
                throw new ArgumentNullException("acquiredLockServices");
            }

            if (acquiredLockServices.Length == 0)
            {
                throw new ArgumentOutOfRangeException("acquiredLockServices");
            }

            return new ReserveResult
            {
                IsSuccess = true,
                ReservedList = acquiredLockServices,
            };
        }

        public static ReserveResult FailResult()
        {
            return new ReserveResult
            {
                IsSuccess = false,
            };
        }

        public static ReserveResult FromValueResult(bool isSuccess, AcquiredLockServices[] acquiredLockServices)
        {
            return new ReserveResult
            {
                IsSuccess = true,
                ReservedList = acquiredLockServices,
            };
        }

    }

    public class DbClusterReservateSettings
    {
        public TimeSpan PingDbTimeout { get; private set; }
        public TimeSpan SearchDbTimeout { get; private set; }
        // потеря бд
        public TimeSpan LostDbTimeout { get; private set; }
        public TimeSpan RetryCurrentDbTimeout { get; private set; }

        public DbClusterReservateSettings(TimeSpan? pingDbTimeout, TimeSpan? searchDbTimeout, TimeSpan? lostDbTimeout, TimeSpan? retryCurrentDbTimeout)
        {
            this.PingDbTimeout = pingDbTimeout ?? TimeSpan.FromMilliseconds(300);
            this.SearchDbTimeout = searchDbTimeout ?? TimeSpan.FromMilliseconds(300);
            this.LostDbTimeout = lostDbTimeout ?? TimeSpan.FromSeconds(7);
            this.RetryCurrentDbTimeout = retryCurrentDbTimeout ?? TimeSpan.FromSeconds(7);
        }
    }

    /// <remarks>
    /// Сервис "получения" И управления состоянием lease-локов для набора("кластера") баз данных
    /// </remarks>
    public interface IDbClusterService : IDisposable
    {
        Task<bool> CheckIfHasLeaseAsync(CancellationToken token);
        //Task<bool> PingAsync(CancellationToken token);
        Task<ReserveResult> RenewLeaseAsync(CancellationToken token);
    }

    /// <remarks>
    /// сервис для "получения" lease-локов для набора("кластера") баз данных (пока реализован ограничено)
    /// </remarks>
    public class DbClusterLeaseService : IDbClusterService
    {
        private IDbClusterUtils dbClusterService;
        private readonly DbClusterParameters dbClusterParameters;
        private AcquiredLockServices dbServices;
        private readonly DbClusterReservateSettings settings;
        private readonly object sync = new object();
        private bool isDisposed = false;

        private CancellationTokenSource onDisposeCancelSource;

        public DbClusterLeaseService(IDbClusterUtils dbClusterService, DbClusterParameters dbClusterParameters, DbClusterReservateSettings settings)
        {
            this.onDisposeCancelSource = new CancellationTokenSource();
            this.dbClusterService = dbClusterService;
            this.dbClusterParameters = dbClusterParameters;
            this.settings = settings;
        }

        public void StartSearchTask(CancellationToken token)
        {
            Task.Factory.StartNew(async () => await this.SearchTaskBody(token), TaskCreationOptions.LongRunning);
        }

        //
        private async Task SearchTaskBody(CancellationToken token)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, this.onDisposeCancelSource.Token))
            {
                //
                var cancellationToken = cts.Token;

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {                        
                        lock (this.sync)
                        {
                            // резервируем бд
                            if (this.dbServices != null && this.dbServices.LockObject.CheckAndUpdateLease())
                            {
                                // lease найдена и действует
                                var leaseLostToken = this.dbServices.LockObject.GetLoseLeaseToken();
                                // ждём потери или завершения
                                WaitHandle.WaitAny(new WaitHandle[] { leaseLostToken.WaitHandle, cancellationToken.WaitHandle });
                                continue;
                            }
                            else
                            {
                                // lease НЕ найдена или НЕ действует
                                this.dbServices?.Dispose();
                                this.dbServices = this.dbClusterService.AcquireServicesForAnyUrlsOrDefault(dbClusterParameters, cancellationToken);
                            }
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // lease не зарезервировалась
                        await Task.Delay(this.settings.SearchDbTimeout, cancellationToken);
                    }
                    catch (OperationCanceledException) when(cancellationToken.IsCancellationRequested)
                    {
                        // завершаем
                        return;
                    }
                    catch (Exception)
                    {
                        // ignore ??
                    }
                }
            }
        }

        public async Task<ReserveResult> RenewLeaseAsync(CancellationToken token)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("dbServices");
            }

            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("dbServices");
                }

                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, this.onDisposeCancelSource.Token))
                {
                    //
                    cts.CancelAfter(settings.LostDbTimeout);

                    var cancellationToken = cts.Token;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            // резервируем бд
                            if (this.dbServices != null && this.dbServices.LockObject.CheckAndUpdateLease())
                            {
                                return ReserveResult.FromValueResult(true, new[] { this.dbServices });
                            }
                            else
                            {
                                // lease НЕ найдена или НЕ действует
                                this.dbServices?.Dispose();
                                this.dbServices = this.dbClusterService.AcquireServicesForAnyUrlsOrDefault(dbClusterParameters, cancellationToken);
                            }

                            // резервируем бд
                            if (this.dbServices != null && this.dbServices.LockObject.CheckAndUpdateLease())
                            {
                                // lease найдена и действует
                            }
                            else
                            {
                                // lease НЕ найдена или НЕ действует

                                // lease потеряна, попробуем восстановить
                                if (this.dbServices?.LockObject.CheckAndUpdateOrAcquireLease(cancellationToken) == true)
                                {

                                }
                                else
                                {
                                    // lease не востановлена или была не найдена, попробуем зарезервировать
                                    this.dbServices?.Dispose();
                                    this.dbServices = this.dbClusterService.AcquireServicesForAnyUrlsOrDefault(dbClusterParameters, cancellationToken);
                                }
                            }

                            //// резервируем бд
                            //if (this.dbServices != null)
                            //{
                            //    if (this.dbServices.LockObject.CheckIfHasLease())
                            //    {
                            //        // lease ещё действует
                            //        return ReserveResult.SuccessResult(this.dbServices);
                            //    }
                            //    else
                            //    {
                            //        // lease потеряна, попробуем восстановить
                            //        if (!this.dbServices.LockObject.CheckAndUpdateOrAcquireLease(cancellationToken))
                            //        {
                            //            // lease потеряна, ищем новую
                            //            this.dbServices?.Dispose();
                            //            this.dbServices = this.dbClusterService.AcquireServicesForAnyUrlsOrDefault(this.dbClusterParameters, cancellationToken);
                            //        }
                            //        else
                            //        {
                            //            // lease действует
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            //    // lease ещё не резервировалась
                            //    this.dbServices = this.dbClusterService.AcquireServicesForAnyUrlsOrDefault(this.dbClusterParameters, cancellationToken);
                            //}

                            // lease не зарезервировалась
                            if (this.dbServices == null)
                            {
                                var searchDelay = this.settings.SearchDbTimeout;

                                //
                                Task.Delay(searchDelay, cancellationToken).Wait(cancellationToken);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    return ReserveResult.FailResult();
                                }

                                continue;
                            }
                            else
                            {
                                return ReserveResult.FromValueResult(this.dbServices.LockObject.CheckAndUpdateLease(), new[] { this.dbServices });
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            return ReserveResult.FailResult();
                        }
                        catch (Exception)
                        {
                            // ignore ??
                        }
                    }

                    return ReserveResult.FailResult();
                }
            }
        }

        public void Dispose()
        {
            this.onDisposeCancelSource?.Cancel();
            lock (this.sync)
            {
                this.dbServices?.Dispose();
                this.onDisposeCancelSource?.Dispose();
                this.isDisposed = true;
            }
        }

        public async Task<bool> CheckIfHasLeaseAsync(CancellationToken token)
        {
            return this.dbServices?.LockObject.CheckLease() ?? false;
        }
    }

    public class MongoDbService : IDbService, IDisposable
    {
        private IMongoDatabase database;
        private IMongoCollection<Lease> leaseCollection;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;
        private readonly ITimeService timeService;
        private readonly ILockService lockService;

        public MongoDbService(string connString,
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
            this.lockService = new MongoDbLeaseLockService(connString, databaseName, leaseCheckTimeout, leaseLostTimeout, this.timeService);
        }

        public void Dispose()
        {
            (this.timeService as IDisposable)?.Dispose();
            (this.lockService as IDisposable)?.Dispose();
        }

        public DateTime GetCurrentUtcTime()
        {
            return this.timeService.GetCurrentUtcTime();
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
