using Atoll.UtilsBundle.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Diagnostics;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
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
        private readonly CancellationTokenSource disposeCts;
        private readonly object disposeLock = new object();


        public MongoDatabaseTimeService(string connString, string databaseName, TimeSpan updateTimeInterval/*, bool startUpdateTimer*/)
        {
            var client = new MongoClient(connString);
            this.database = client.GetDatabase(databaseName);
            this.updateTimeInterval = updateTimeInterval;
            this.disposeCts = new CancellationTokenSource();
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
                lock (this.DatesLockObj)
                {
                    this.disposeCts.SafeDispose();
                    this.stopwatch?.Stop();
                    this.updateDatetimeTimer?.Dispose();
                    // UpdateTime завязан на null
                    this.updateDatetimeTimer = null;

                    this.isDisposed = true;
                }
            }            
        }

        private static TimeSpan maxAllowedDelta = TimeSpan.FromSeconds(1);

        private void UpdateTimerCallBack(object state)
        {
            try
            {
                this.UpdateTime(CancellationToken.None);
            }
            catch (Exception)
            {
                // TODO insrumetation ?
            }     
        }

        private void UpdateTime(CancellationToken cancellationToken)
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
                var dbReturnedTime = this.GetCurrentUtcFromMongoServer(cancellationToken);
                var newStopWatch = Stopwatch.StartNew();
                requestStopWatch.Stop();
                var requestTimeSpan = requestStopWatch.Elapsed;
                if (requestTimeSpan > maxAllowedDelta)
                {
                    // TODO instrumentation ? retry ? Throw ?
                }

                var dbTime = dbReturnedTime + TimeSpan.FromTicks(requestTimeSpan.Ticks / 2);

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
        private DateTime GetCurrentUtcFromMongoServer(CancellationToken cancellationToken)
        {
            // на данный момент в Mongo отсутствует оператор текущего timestamp/datetime который может работать в запросах или который можно вернуть в результате (есть $currentDate для Update и $$NOW для aggregation pipeline)
            // https://docs.mongodb.com/manual/reference/command/hostInfo/
            // hostInfo.system.currentTime. A timestamp of the current system time.
            // также возможно использовать serverStatus, но hostInfo быстрее работает
            // с исполнением eval не стал разбираться - Command eval failed: no such command: 'eval'.
            // var result = this.database.EvalAsync("return new Date()").Result;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, disposeCts.Token))
            {
                cts.CancelAfter(this.updateTimeInterval);

                var cmdResult = this.database.RunCommand(hostInfoCmd, null, cts.Token);
                var result = cmdResult["system"]["currentTime"].ToUniversalTime();
                return result;
            }
        }

        public DateTime GetCurrentUtcTime(CancellationToken cancellationToken)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("TimeService");
            }

            lock (this.DatesLockObj)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("TimeService");
                }

                if (this.etalonTime == null)
                {
                    // возможно стоит добавить ограничение на время выполнени этого метода
                    this.UpdateTime(cancellationToken);
                    this.StartTimerIfNotStarted();
                }

                return this.etalonTime.Value + this.stopwatch.Elapsed;
            }
        }
    }

}
