using Atoll.Transport;
using Atoll.Transport.ServerBundle;
using Atoll.UtilsBundle.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataProcessing
{

    /// <summary>
    /// Взаимодействие с узлом сбора событий.
    /// </summary>
    internal sealed class DataNodeReader: IDisposable
    {
        internal readonly DataNodeDefinition nodeDefinition;
        private readonly MongoClient client;
        private IMongoDatabase database;
        private IMongoCollection<GlobalSetting> settingsCollection;
        private IMongoCollection<PacketPart> packetsPartsCollection;
        private ITimeService timeService;
        private readonly object disposeLock = new object();
        private bool isDisposed;
        private readonly CancellationTokenSource disposeCts;

        private int fullRangeScanCounter = 1;
        private TimeSpan warnLimitForSelecting = TimeSpan.FromMilliseconds(500);
        private long lastFinishTransferDateTimeTicksForQuery = 0;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="dhuNodeDefinition">Конфигурация узла.</param>
        public DataNodeReader(DataNodeDefinition dhuNodeDefinition, IDbServicesFactory timeServiceFactory)
        {
            if (dhuNodeDefinition == null)
                throw new ArgumentNullException("dhuNodeDefinition");

            this.nodeDefinition = dhuNodeDefinition;

            var url = this.nodeDefinition.ServiceUri.ToString();
            this.client = new MongoClient(url);
            this.database = client.GetDatabase(TransportConstants.MongoDatabaseName);
            this.settingsCollection = this.database.GetCollection<GlobalSetting>(TransportConstants.GlobalSettingTable);

            this.packetsPartsCollection = this.database.GetCollection<PacketPart>(TransportConstants.PacketsPartsTable);
            this.timeService = timeServiceFactory.GetTimeService(url, TransportConstants.MongoDatabaseName, TimeSpan.FromMinutes(30));
            this.disposeCts = new CancellationTokenSource();
        }

        public Uri ServiceUri { get { return this.nodeDefinition.ServiceUri; } }

        public string StorageIdentifier { get; private set; }

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
                this.timeService.SafeDispose();

                this.isDisposed = true;
            }        
        }

        private async Task<string> RequestStorageToken(CancellationToken cancellationToken)
        {
            try
            {
                var filter = Builders<GlobalSetting>.Filter.Eq(x => x.Id, TransportConstants.StorageTokenKey);
                var storageToken = await (await this.settingsCollection.FindAsync<GlobalSetting>(filter, null, cancellationToken)).FirstOrDefaultAsync(cancellationToken);
                return storageToken?.Token;
            }
            catch (Exception exception)
            {
                //DataProcessingProfilingUnit.Unit.DhuNodeReaderRequestContainerIdException(serviceUri, DObjectSnapshotProducer.MakeSnapshot(exception));
                return null;
            }
        }

        private CancellationTokenSource GetCancelationSource(CancellationToken token)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(token, this.disposeCts.Token);
        }

        public async Task InitializeAsync(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            using (var cts = this.GetCancelationSource(token))
            {
                var cancellationToken = cts.Token;

                var delayTimeout = TimeSpan.FromSeconds(15);
                var delayIncrement = TimeSpan.FromSeconds(60);
                var maxTimeout = TimeSpan.FromSeconds(300);

                while (this.StorageIdentifier == null)
                {
                    var requestedId = await RequestStorageToken(cancellationToken);

                    if (requestedId != null)
                    {
                        try
                        {
                            await new MongoIndexCreator().InitIndexes(this.database, cancellationToken);
                        }
                        catch (Exception)
                        {
                            // TODO instrumentation
                        }

                        this.StorageIdentifier = requestedId;
                        //DataProcessingProfilingUnit.Unit.DhuNodeReaderInitializationCompleted(this.ServiceUri, this.ContainerId);
                        return;
                    }

                    await Task.Delay(delayTimeout, cancellationToken);

                    delayTimeout = delayTimeout + delayIncrement;
                    if (delayTimeout > maxTimeout)
                        delayTimeout = maxTimeout;
                }
            }
        }

        public void DeleteCompletedOrExpaired(TimeSpan packetsLifeTime, CancellationToken cancellationToken)
        {
            this.RunMongoOps((ct) => this.DeleteCompletedOrExpairedInternal(packetsLifeTime, ct), cancellationToken);
        }

        private const int deleteLimit = 2000;

        private void DeleteCompletedOrExpairedInternal(TimeSpan packetsLifeTime, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // TODO стоит заменить на capped коллекцию ?
            // https://jira.mongodb.org/browse/SERVER-4796
            var filterToCheck = !Builders<PacketPart>.Filter
                                                     .Eq(x => x.EndTime, null) 
                                | Builders<PacketPart>.Filter
                                                      .Lt(x => x.CreatedTime, DateTime.UtcNow - packetsLifeTime);

            var finded = this.packetsPartsCollection
                    .Find(filterToCheck)
                    .Project(x => x.PacketId)
                    .Limit(deleteLimit)
                    .ToList(cancellationToken)
                    .Distinct();

            while (!cancellationToken.IsCancellationRequested && finded.Any())
            {
                var deleteFilter = Builders<PacketPart>.Filter.In(x => x.PacketId, finded);
                this.packetsPartsCollection.DeleteMany(deleteFilter, cancellationToken);

                finded = this.packetsPartsCollection
                    .Find(filterToCheck)
                    .Project(x => x.PacketId)
                    .Limit(deleteLimit)
                    .ToList(cancellationToken)
                    .Distinct();
            }
        }

        // Change streams are available for replica sets and sharded clusters
        // https://docs.mongodb.com/manual/changeStreams/
        //public async Task WaitForChange(CancellationToken cancellationToken)
        //{
        //    var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<PacketPart>>()
        //            .Match(x => x.OperationType == ChangeStreamOperationType.Insert && x.FullDocument.IsFinal == true);

        //    using (var cursor = await this.packetsPartsCollection.WatchAsync(pipeline))
        //    {
        //        // process insert change event
        //        await cursor.AnyAsync();
        //    }
        //}

        private async Task<T> RunMongoOpsAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken)
        {
            try
            {
                using (var cts = this.GetCancelationSource(cancellationToken))
                {
                    return await func(cts.Token);
                }
            }
            catch (Exception)
            {
                this.ReInitializeAsync(this.disposeCts.Token);
                throw;
            }
        }

        private T RunMongoOps<T>(Func<CancellationToken, T> func, CancellationToken cancellationToken)
        {
            try
            {
                using (var cts = this.GetCancelationSource(cancellationToken))
                {
                    return func(cts.Token);
                }
            }
            catch (Exception)
            {
                this.ReInitializeAsync(this.disposeCts.Token);
                throw;
            }
        }

        private void RunMongoOps(Action<CancellationToken> action, CancellationToken cancellationToken)
        {
            try
            {
                using (var cts = this.GetCancelationSource(cancellationToken))
                {
                    action(cts.Token);
                }
            }
            catch (Exception)
            {
                // TODO instrumentation
                this.ReInitializeAsync(this.disposeCts.Token);
                throw;
            }
        }

        public ReservedRange ReserveRangeAsync(string dpuId, ReserveRangeOptions options, CancellationToken cancellationToken)
        {
            return this.RunMongoOps((token) => this.ReserveRangeInternal(dpuId, options, token), cancellationToken);
        }
 
        private async Task ReInitializeAsync(CancellationToken cancellationToken)
        {
            this.StorageIdentifier = null;
            await this.InitializeAsync(cancellationToken);
        }

        private static FilterDefinition<PacketPart> notEndedFilter = Builders<PacketPart>.Filter.Eq(x => x.EndTime, null);
        private static FilterDefinition<PacketPart> notStartedFilter = Builders<PacketPart>.Filter.Eq(x => x.StartTime, null);
        private static FilterDefinition<PacketPart> assigned = ((!Builders<PacketPart>.Filter.Eq(x => x.ProcessingUnitId, null)) & notEndedFilter);
        private static FilterDefinition<PacketPart> hasFinishTimeAndFinal = !Builders<PacketPart>.Filter.Eq(x => x.FinalPartTransferTime, null);

        class ReserveQueryFilters
        {
            public FilterDefinition<PacketPart> ForReservationFilter { get; }
            public FilterDefinition<PacketPart> AlreadyAssignedOnCurrentFilter { get; }

            public ReserveQueryFilters(FilterDefinition<PacketPart> forReservationFilter, FilterDefinition<PacketPart> alreadyAssignedOnCurrentFilter)
            {
                this.ForReservationFilter = forReservationFilter;
                this.AlreadyAssignedOnCurrentFilter = alreadyAssignedOnCurrentFilter;
            }
        }

        private ReserveQueryFilters GetFiltersForReserveQuery(bool isFullScanQuery, string dpuId, ReserveRangeOptions options, CancellationToken cancellationToken)
        {
            FilterDefinition<PacketPart> assignedOnCurrentDpuFilter = Builders<PacketPart>.Filter
                                .Eq(x => x.ProcessingUnitId, dpuId);

            var update = Builders<PacketPart>.Update
                .Set(x => x.ProcessingUnitId, dpuId)
                // $currentDate
                .CurrentDate(x => x.StartTime);

            FilterDefinition<PacketPart> alreadyAssignedOnCurrentFilter = assignedOnCurrentDpuFilter & notEndedFilter;

            // Пока DateTime.UtcNow отсутствует для запросов фильтрации (есть CurrentDate для Update и $$NOW для aggregation pipeline)
            // https://jira.mongodb.org/browse/SERVER-23656
            // https://stackoverflow.com/questions/20620368/is-there-any-equivalent-of-now-in-mongodb
            // т.к. lease у нас имеет относительно большое время жизни здесь мы указываем фиксированную дату для условия, но по сути, её следует обновлять при выполнении запросов (а лучше заменить на аналог NOW() и тп когда он появится)
            var leaseExpairedFilter = Builders<PacketPart>.Filter
                .Lt(x => x.StartTime, (timeService.GetCurrentUtcTime(cancellationToken) - options.LeaseTimeout).ToUniversalTime());

            // есть дополнительные условия для использования индексов
            var canBeReassignedFilter = assigned & leaseExpairedFilter;

            var lastFinishTransferDateTimeTicks = this.lastFinishTransferDateTimeTicksForQuery;

            // FinalPartTransferTime != null - ищем только среди последних частей
            // addGteFinishTime - в случае когда записей много и нужно выбрать только те которые нужны "следующие" для обработки, без этого условия (котрое добавляется при addGteFinishTime==true) мы получаем слишком большое время выполнения запроса
            var addGteFinishTime = !isFullScanQuery && lastFinishTransferDateTimeTicks != 0;
            var commonFilter = addGteFinishTime
                    ? hasFinishTimeAndFinal & Builders<PacketPart>.Filter
                        .Gte(x => x.FinalPartTransferTime, new DateTime(lastFinishTransferDateTimeTicks))
                    : hasFinishTimeAndFinal;

            return new ReserveQueryFilters(commonFilter &
                    (
                    alreadyAssignedOnCurrentFilter
                    | canBeReassignedFilter
                    // next to process
                    | notStartedFilter
                    ), alreadyAssignedOnCurrentFilter);
        }

        /// <remarks>метод при обращении к бд использует индекс <see cref="MongoIndexCreator.CompoundIndexName"/></remarks>
        private ReservedRange ReserveRangeInternal(string dpuId, ReserveRangeOptions options, CancellationToken cancellationToken)
        {            
            var packetsParts = this.ReservePackets(dpuId, options, cancellationToken);

            return new ReservedRange
            {
                DataNodeToFinalParts = new Dictionary<DataNodeDefinition, List<AgentPacketPartInfo>>()
                {
                    { this.nodeDefinition, packetsParts }
                }
            };
        }

        private static ProjectionDefinition<PacketPart, AgentPacketPartInfo> reservePacketsProjection = Builders<PacketPart>.Projection
                .Exclude(x => x.Bytes)
                .Exclude(x => x.ProcessingUnitId)
                .Exclude(x => x.StartTime)
                .Exclude(x => x.EndTime)
                .Exclude(x => x.CreatedTime);

        private static SortDefinition<PacketPart> reservePacketsSortDef = Builders<PacketPart>.Sort
                //.Ascending(x => x.PacketId)
                .Ascending(x => x.FinalPartTransferTime);

        
        private List<AgentPacketPartInfo> ReservePackets(
            string dpuId, ReserveRangeOptions options, CancellationToken cancellationToken)
        {
            var needReserveRange = options.NeedReserveRange;
            var packetsParts = new List<AgentPacketPartInfo>();
            var update = Builders<PacketPart>.Update
                .Set(x => x.ProcessingUnitId, dpuId)
                // $currentDate
                .CurrentDate(x => x.StartTime);

            var isFullScanQuery = (Interlocked.Increment(ref this.fullRangeScanCounter) % options.FullScanAfterNQueries) == 0;
            // в mongo нет аналога select for update, поэтому пока используется такая реализация
            // из альтенатив можно использовать транзакции, но они не атомарны для read+update, ещё есть вариант с $isolated
            // при запуске
            // при оптимизации сортировки может быть интересно
            // https://docs.mongodb.com/manual/reference/operator/aggregation/sort/index.html#sort-operator-and-memory
            // https://stackoverflow.com/questions/52019790/sort-makes-my-query-too-slow-in-mongodb
            var filters = this.GetFiltersForReserveQuery(isFullScanQuery, dpuId, options, cancellationToken);

            // в mongo 
            var stopWatch = Stopwatch.StartNew();
            var findedForAssigment = this.packetsPartsCollection
                .Find(filters.ForReservationFilter)
                .Sort(reservePacketsSortDef)
                .Limit(needReserveRange.Max)
                // проекция иногда снижает производительность ??? (был странный момент когда производительность падала в десятки раз, но воспроизвести я это не смог)
                .Project(reservePacketsProjection)
                .ToList(cancellationToken);
            stopWatch.Stop();

            if (!isFullScanQuery && stopWatch.Elapsed > warnLimitForSelecting)
            {
                // TODO instrumentation
                Debug.WriteLine($"find not processed query time - {stopWatch.Elapsed}");
            }

            // TODO эту часть стоит улучшить\изменить (Min не учитывается, хотя по сути должен)!
            while (findedForAssigment.Any() && needReserveRange.Min > packetsParts.Count)
            {
                var idsFilter = Builders<PacketPart>.Filter.In(x => x.Id, findedForAssigment.Select(x => x.Id));
                var toUpdateFilter = filters.ForReservationFilter & idsFilter;
                var updateResult = this.packetsPartsCollection.UpdateMany(toUpdateFilter, update, null, cancellationToken);

                if (findedForAssigment.Count == updateResult.ModifiedCount || updateResult.MatchedCount == 0)
                {
                    packetsParts = findedForAssigment;
                    break;
                }
                else
                {
                    findedForAssigment = this.packetsPartsCollection
                        .Find(idsFilter & filters.AlreadyAssignedOnCurrentFilter)
                        .Sort(reservePacketsSortDef)
                        .Limit(needReserveRange.Max)
                        .Project(reservePacketsProjection)
                        .ToList(cancellationToken);
                    //break;
                }
            }

            var newVal = findedForAssigment.Max(x => x.FinalPartTransferTime);
            if (newVal != null)
            {
                InterlockedExtension.AssignIfNewValueBigger(ref this.lastFinishTransferDateTimeTicksForQuery, newVal.Value.Ticks);
            }

            return findedForAssigment;
        }

        public void Complete(IList<AgentPacketPartInfo> values, CancellationToken cancellationToken)
        {
            this.RunMongoOps((token) => this.CompleteInternal(values, token), cancellationToken);
        }

        private void CompleteInternal(IList<AgentPacketPartInfo> values, CancellationToken cancellationToken)
        {
            var filter = Builders<PacketPart>.Filter.In(x => x.PacketId, values.Select(x => x.PacketId));
            var update = Builders<PacketPart>.Update.CurrentDate(x => x.EndTime);
            this.packetsPartsCollection.UpdateMany(filter, update, null, cancellationToken);
            //this.packetsPartsCollection.DeleteMany(filter, null, cancellationToken);
        }

        public IList<PacketPartInfo> GetPacketPartInfos(IList<string> packetIds, CancellationToken cancellationToken)
        {
            return this.RunMongoOps((token) => this.GetPacketPartInfosInternal(packetIds, token), cancellationToken);
        }

        private static ProjectionDefinition<PacketPart, PacketPartInfo> packetPartInfosInternalProjection = Builders<PacketPart>.Projection
                 .Exclude(x => x.Bytes)
                .Exclude(x => x.ProcessingUnitId)
                .Exclude(x => x.StartTime)
                .Exclude(x => x.EndTime)
                .Exclude(x => x.CreatedTime)
                .Exclude(x => x.AgentInfo);
        private IList<PacketPartInfo> GetPacketPartInfosInternal(IList<string> packetIds, CancellationToken cancellationToken)
        {
            var filter = Builders<PacketPart>.Filter.In(x => x.PacketId, packetIds);
            return (this.packetsPartsCollection.Find(filter).Project<PacketPartInfo>(packetPartInfosInternalProjection)).ToList(cancellationToken);
        }

        //public byte[] GetPacketPartInfoBytes(string id, CancellationToken cancellationToken)
        //{
        //    return this.RunMongoOps((token) => this.GetPacketPartInfoBytesInternal(id, token), cancellationToken);
        //}

        public IDictionary<string, byte[]> GetPacketPartInfoBytes(IEnumerable<string> ids, CancellationToken cancellationToken)
        {
            return this.RunMongoOps((token) => this.GetPacketPartInfoBytesInternal(ids, token), cancellationToken);
        }

        //public byte[] GetPacketPartInfoBytesInternal(string id, CancellationToken cancellationToken)
        //{
        //    var filter = Builders<PacketPart>.Filter.Eq(x => x.Id, id);
        //    // с проекцией работает чуток медленней, что на самом деле странно
        //    return this.packetsPartsCollection.Find(filter).Project(x => x.Bytes).FirstOrDefault(cancellationToken);
        //    //return this.packetsPartsCollection.Find(filter).FirstOrDefault(cancellationToken)?.Bytes;
        //}

        public IDictionary<string, byte[]> GetPacketPartInfoBytesInternal(IEnumerable<string> ids, CancellationToken cancellationToken)
        {
            var filter = Builders<PacketPart>.Filter.In(x => x.Id, ids);
            // с проекцией работает чуток медленней, что на самом деле "странно"
            return this.packetsPartsCollection
                .Find(filter)
                //.Project(x => new { Id = x.Id, Bytes = x.Bytes })
                .ToList(cancellationToken)
                .Select(x => new { Id = x.Id, Bytes = x.Bytes })
                .ToDictionary(x => x.Id, x=> x.Bytes);
        }

    }

    public static class InterlockedExtension
    {
        public static bool AssignIfNewValueSmaller(ref long target, long newValue)
        {
            long snapshot;
            bool stillLess;
            do
            {
                snapshot = target;
                stillLess = newValue < snapshot;
            } while (stillLess && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillLess;
        }

        public static bool AssignIfNewValueBigger(ref long target, long newValue)
        {
            long snapshot;
            bool stillMore;
            do
            {
                snapshot = target;
                stillMore = newValue > snapshot;
            } while (stillMore && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillMore;
        }
    }
}
