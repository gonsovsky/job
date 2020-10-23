using Atoll.Transport.ServerBundle;
using Atoll.UtilsBundle.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataProcessing
{
    public static class RandomizationExtensions
    {

        private static Random rng = new Random();
        // https://stackoverflow.com/questions/273313/randomize-a-listt/1262619
        // https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class ProcessingService
    {

        public class AgentPacketPartNodePair
        {

            public AgentPacketPartInfo PacketPartInfo { get; set; }

            public DataNodeDefinition NodeDefinition { get; set; }

            public PacketPartNodePair ToPacketPartNodePair()
            {
                return new PacketPartNodePair
                {
                    PacketPartInfo = this.PacketPartInfo,
                    NodeDefinition = this.NodeDefinition,
                };
            }
        }

        private readonly ProcessingSettings settings;
        private readonly IEnumerable<IProcessorManager> processorManagers;
        private IDbServicesFactory dbServicesFactory;
        private DataNodeReadersPool dataNodeReadersPool;
        private IDictionary<string, ProcessorContainer[]> processorContainers;
        private IUnitIdProvider dpuIdProvider;

        public ProcessingService(IUnitIdProvider dpuIdProvider, IEnumerable<IProcessorManager> processorManagers, ProcessingSettings settings)
        {
            this.dpuIdProvider = dpuIdProvider;
            this.processorManagers = processorManagers;
            this.settings = settings;
        }


        public void OnCreate()
        {
            //пул читателей DHU
            var dhuNodeDefinitions = settings.DataNodes;

            this.dbServicesFactory = new MongoDbServicesFactory(this.dpuIdProvider);
            this.dataNodeReadersPool = new DataNodeReadersPool(dhuNodeDefinitions, this.settings.PriorityNodes, this.dbServicesFactory);

            //DataProcessingProfilingUnit.Unit.KnownDhuNodes(DObjectSnapshotProducer.MakeSnapshot(dhuNodeDefinitions.Select(x => x.ServiceUri)));

            //фабрики контейнеров процессоров
            this.processorContainers = this.processorManagers
                .Where(p => p.IsEnabled)
                .GroupBy(x => x.Id)
                .ToDictionary(p => p.Key, p => p.Select(x => x.GetProcessorContainerFactory().GetProcessorContainer()).ToArray());

            //DataProcessingProfilingUnit.Unit.KnownDpuProcessors(DObjectSnapshotProducer.MakeSnapshot(processorFactories.Select(x => x.Id)));

            var connectionAppName = this.GetRandomSqlConnectionAppName();
            //DataProcessingProfilingUnit.Unit.ConnectionAppName(connectionAppName);
            var connectionString = this.settings.PcdConnectionString + connectionAppName;
        }

        private readonly object syncRoot = new object();
        private CancellationTokenSource cancellationTokenSource;
        private Task processingTask;
        private Task deletionTask;

        public object StopWatch { get; private set; }

        public void OnStart()
        {
            lock (this.syncRoot)
            {
                if (this.cancellationTokenSource == null || this.cancellationTokenSource.IsCancellationRequested)
                {
                    this.cancellationTokenSource?.Dispose();

                    this.cancellationTokenSource = new CancellationTokenSource();

                    this.processingTask = Task.Factory.StartNew(this.Processing, this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    if (this.settings.DeleteProcessedRecordsTimeout != null)
                    {
                        this.deletionTask = Task.Factory.StartNew(this.DeleteProcessed, this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                    }
                }
            }
        }

        public void OnStop()
        {
            lock (this.syncRoot)
            {
                if (this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested)
                {
                    this.cancellationTokenSource.SafeCancel();
                    this.processingTask?.Wait();
                    this.deletionTask?.Wait();
                }
            }
        }

        private void OnDispose()
        {
            this.cancellationTokenSource.SafeCancel();
            this.processingTask?.Wait();
            this.deletionTask?.Wait();
            this.cancellationTokenSource.SafeDispose();
            this.dataNodeReadersPool.SafeDispose();
        }

        private void Init(CancellationToken cancellationToken)
        {
            try
            {
                // ининциализация ридеров
                this.dataNodeReadersPool.InitReaders(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // отмена
                return;
            }
        }

        private void DeleteProcessed()
        {
            var cancellationToken = this.cancellationTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                this.DeleteFromNodes(cancellationToken);
                Task.Delay(this.settings.DeleteProcessedRecordsTimeout.Value, cancellationToken).Wait(cancellationToken);
            }
        }

        private void DeleteFromNodes(CancellationToken cancellationToken)
        {
            foreach (var node in this.GetDataNodesOrderedForQuering())
            {
                if (this.dataNodeReadersPool.TryGetReader(node, out var dataReader))
                {
                    dataReader.DeleteCompletedOrExpaired(this.settings.PacketsLifeTime, cancellationToken);
                }
                else
                {
                    this.InstrumentateNoReaderForDef(node, "Не найден DataReader для резервирования записей для обработки");
                }
            }
        }

        private void Processing()
        {
            var cancellationToken = this.cancellationTokenSource.Token;

            this.Init(cancellationToken);

            TimeSpan minDelay = TimeSpan.FromSeconds(3);
            TimeSpan maxDelay = TimeSpan.FromSeconds(10);
            TimeSpan delayStep = TimeSpan.FromSeconds(1);
            TimeSpan currentDelay = minDelay;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool canSuspend = false;
                    using (var cancelationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                    {
                        // таймаут ?
                        //cancelationSource.CancelAfter

                        var dpuId = this.dpuIdProvider.GetId();
                        // NeedReserveCount можно считать на основе оперативки ?
                        //    PerformanceCounter ramCounter;
                        //    ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                        //    ramCounter.NextValue();

                        var context = new DataProcessingContext
                        {
                            DpuId = dpuId,
                            OrderedNodes = this.GetDataNodesOrderedForQuering(),
                            CancellationToken = cancelationSource.Token,
                            NeedReserveRange = this.settings.ReservationRangeLimits,
                            LoadedInMemoryBinaryPartsBatchSize = this.settings.LoadedInMemoryBinaryPartsBatchSize,
                            HasDataInPreviousProcessing = !canSuspend,
                        };

                        var stopWatch = Stopwatch.StartNew();
                        this.ReserveRange(context);
                        var reservedTime = stopWatch.Elapsed;
                        if (context.HasDataToProcess)
                        {
                            this.ProcessRange(context);
                            var processedTime = stopWatch.Elapsed - reservedTime;
                            this.CompleteRange(context);
                            var completeRangeTime = stopWatch.Elapsed - processedTime - reservedTime;
                            Debug.WriteLine($"timings: {reservedTime}, {processedTime}, {completeRangeTime}");
                            canSuspend = false;
                        }
                        else
                        {
                            canSuspend = true;
                        }


                        if (canSuspend)
                        {
                            Debug.WriteLine($"processing without suspention ended, time spent - {stopWatch.Elapsed}");
                            if (!context.HasDataInPreviousProcessing)
                            {
                                if (currentDelay < maxDelay)
                                {
                                    currentDelay += delayStep;
                                }
                            }
                        }
                        else
                        {
                            currentDelay = minDelay;
                        }
                    }

                    // возможность приостановить цикл, если не было данных для обработки
                    if (canSuspend)
                    {
                        Task.Delay(currentDelay, cancellationToken).Wait(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // TODO Instrumentation
                }
                catch (Exception)
                {
                    // ignore
                    // TODO Instrumentation
                }
            }
        }

        private void CompleteRange(DataProcessingContext ctx)
        {
            foreach (var item in ctx.ReservedRange.DataNodeToFinalParts)
            {
                if (this.dataNodeReadersPool.TryGetReader(item.Key, out var dataReader))
                {
                    dataReader.Complete(item.Value, ctx.CancellationToken);
                }
                else
                {
                    this.InstrumentateNoReaderForDef(item.Key, "Не найден DataReader для завершения обработки");
                }
            }
        }


        private IEnumerable<DataNodeDefinition> GetDataNodesOrderedForQuering()
        {
            // метод чуток рандомизирует (балансирует) нагрузку (сделано в таком упрошённом виде...)
            IEnumerable<DataNodeDefinition> nodesToQuery = this.settings.DataNodes;

            // random порядок, приорететные узлы первые
            if (settings.PriorityNodes != null && settings.PriorityNodes.Any())
            {
                var nodesList = new List<DataNodeDefinition>();                
                var priorityNodes = settings.PriorityNodes;
                // randomize PriorityNodes ?
                //priorityNodes = priorityNodes.ToArray(); priorityNodes.Shuffle();
                nodesList.AddRange(priorityNodes);
                var nodesToShuffle = nodesToQuery.Except(nodesList).ToList();
                nodesToShuffle.Shuffle();
                nodesList.AddRange(nodesToShuffle);
                nodesToQuery = nodesList;
            }
            else
            {
                var nodesToShuffle = nodesToQuery.ToList();
                nodesToShuffle.Shuffle();
                nodesToQuery = nodesToShuffle;
            }

            return nodesToQuery;
        }

        private void ReserveRange(DataProcessingContext ctx)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            foreach (var node in ctx.OrderedNodes)
            {
                if (this.dataNodeReadersPool.TryGetReader(node, out var dataReader))
                {
                    this.ReserveRange(dataReader, ctx);
                }
                else
                {
                    this.InstrumentateNoReaderForDef(node, "Не найден DataReader для резервирования записей для обработки");
                }
                if (ctx.ReservedRange != null && ctx.IsRangeFilled())
                {
                    break;
                }
                else
                {

                }
            }
        }

        private void ReserveRange(DataNodeReader nodeReader, DataProcessingContext ctx)
        {
            ReservedRange range = nodeReader.ReserveRangeAsync(ctx.DpuId, new ReserveRangeOptions
            {
                LeaseTimeout = this.settings.JobLeaseLifeTime ?? TimeSpan.FromHours(1),
                NeedReserveRange = ctx.NeedReserveRange.ForAlreadyReserved(ctx.RangeRecordsCount()),
                HasDataInPreviousProcessing = ctx.HasDataInPreviousProcessing,
                FullScanAfterNQueries = 20,
            }, ctx.CancellationToken);

            if (ctx.ReservedRange == null)
            {
                ctx.ReservedRange = range;
            }
            else
            {
                ctx.ReservedRange.Add(range);
            }
        }

        private bool HasWorkingProcessors(string providerKey)
        {
            return this.processorContainers.TryGetValue(providerKey, out var procs) && procs.Any(x => x.CanProcess);
        }

        private void ProcessRange(DataProcessingContext ctx)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            // TODO стоит сделать что-нить более удобное...
            foreach (var nodeToPacketsParts in ctx.ReservedRange.DataNodeToFinalParts)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                foreach (var providerGrouping in nodeToPacketsParts.Value
                                                .OrderBy(x => x.PacketId)
                                                .GroupBy(x => x.ProviderKey))
                {
                    // если отсутствуют работющие процессоры то не обрабатываем
                    if (this.HasWorkingProcessors(providerGrouping.Key))
                    {
                        this.FillPacketsInfosChains(nodeToPacketsParts.Key, providerGrouping, ctx);
                        this.ProcessForProvider(providerGrouping.Key, providerGrouping, ctx);
                    }
                }
            }
        }

        private void FillPacketsInfosChains(DataNodeDefinition node, IEnumerable<AgentPacketPartInfo> providerFinalPacketParts, DataProcessingContext ctx)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            var partedFinalParts = new List<AgentPacketPartNodePair>();
            if (this.dataNodeReadersPool.TryGetReader(node, out var dataReader))
            {
                foreach (var packetFinalPartInfo in providerFinalPacketParts)
                {
                    ctx.CancellationToken.ThrowIfCancellationRequested();

                    // для частей которые содержат весь пакет
                    if (packetFinalPartInfo.StartPosition == 0)
                    {
                        ctx.PacketToChain.Add(packetFinalPartInfo, new List<PacketPartNodePair> {
                            new PacketPartNodePair
                            {
                                NodeDefinition = node,
                                PacketPartInfo = packetFinalPartInfo,
                            }
                        });
                        //ctx.PacketToStreamFactory.Add(packetFinalPartInfo, (token) => new MemoryStream(this.GetBytes(node, packetFinalPartInfo, token), false));
                    }
                    else
                    {
                        partedFinalParts.Add(new AgentPacketPartNodePair
                        {
                            NodeDefinition = node,
                            PacketPartInfo = packetFinalPartInfo,
                        });
                    }
                }
            }
            else
            {
                this.InstrumentateNoReaderForDef(node, "Не найден DataReader для скачивания");
            }

            // для пакетов которые разбиты на части
            this.FillNonFullParts(partedFinalParts, ctx);
        }

        // TODO можно собирать PacketPartNodePair не по одной записи, а для всей пачки
        // для пакетов которые разбиты на части
        private void FillNonFullParts(IList<AgentPacketPartNodePair> nonFullPartToNodes, DataProcessingContext ctx)
        {
            var cache = new List<PacketPartNodePair>();
            var checkedStoreTokens = new List<string>();
            foreach (var part in nonFullPartToNodes)
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();

                PacketPartNodePair currentPartInfo = part.ToPacketPartNodePair();

                var chains = new Stack<List<PacketPartNodePair>>();
                chains.Push(new List<PacketPartNodePair>() { currentPartInfo });

                List<PacketPartNodePair> completedChain = null;
                List<PacketPartNodePair> currentChain;
                while (chains.Any() && (currentChain = chains.Pop()) != null)
                {
                    var results = currentChain;
                    currentPartInfo = currentChain.Last();
                    while (currentPartInfo != null && currentPartInfo.PacketPartInfo.PreviousPartId != null)
                    {
                        ctx.CancellationToken.ThrowIfCancellationRequested();
                        if (currentPartInfo.PacketPartInfo.PreviousPartStorageToken == null)
                        {
                            // непонятная ситуация - ошибка
                            currentPartInfo = null;
                            results = null;
                            break;
                        }

                        var currentPartInfos = cache.Where(x => x.PacketPartInfo.Id == currentPartInfo.PacketPartInfo.PreviousPartId);
                        if (!currentPartInfos.Any())
                        {
                            // добавляем в кэш только один раз
                            if (!checkedStoreTokens.Contains(currentPartInfo.PacketPartInfo.PreviousPartStorageToken) && this.dataNodeReadersPool.TryGetReaderForStorageId(currentPartInfo.PacketPartInfo.PreviousPartStorageToken, out var reader))
                            {
                                var infos = reader.GetPacketPartInfos(nonFullPartToNodes.Select(x => x.PacketPartInfo.PacketId).ToList(), ctx.CancellationToken);
                                foreach (var item in infos)
                                {
                                    cache.Add(new PacketPartNodePair
                                    {
                                        PacketPartInfo = item,
                                        NodeDefinition = reader.nodeDefinition,
                                    });
                                }
                                checkedStoreTokens.Add(currentPartInfo.PacketPartInfo.PreviousPartStorageToken);
                                currentPartInfos = cache.Where(x => x.PacketPartInfo.Id == currentPartInfo.PacketPartInfo.PreviousPartId);
                            }
                            else
                            {
                                this.InstrumentateNoReaderForStorageToken(currentPartInfo.PacketPartInfo.PreviousPartStorageToken, "");
                            }
                        }

                        if (currentPartInfos.Any())
                        {
                            currentPartInfo = currentPartInfos.First();
                            results.Add(currentPartInfo);
                            foreach (var item in currentPartInfos.Skip(1))
                            {
                                var chain = results.ToList();
                                chain.Add(item);
                                chains.Push(chain);
                            }
                        }
                        else
                        {
                            // TODO instrumentation
                            currentPartInfo = null;
                            results = null;
                        }
                    }

                    if (this.CheckCompletedChain(results))
                    {
                        results.Reverse();
                        completedChain = results;
                        break;
                    }
                }

                if (completedChain != null)
                {
                    ctx.PacketToChain.Add(part.PacketPartInfo, completedChain);
                }
                else
                {
                    // TODO instrumentation
                }
            }
        }


        private bool CheckCompletedChain(IList<PacketPartNodePair> pairs)
        {
            // проверим уникальность агента ?
            if (pairs != null)
            {
                return true;
            }

            return false;
        }

        private bool TryMakeCompleteChainFromPartInfos(IList<PacketPartNodePair> pairs, out IList<PacketPartNodePair> result)
        {
            // проверим набор частей файла
            var ordered = pairs.OrderBy(x => x.PacketPartInfo.StartPosition).ToList();
            var agents = new HashSet<string>();

            if (agents.Count > 1)
            {
                result = null;
                return false;
            }

            result = ordered;

            return true;
        }

        private IDictionary<AgentPacketPartInfo, byte[]> GetBytesForPacketChains(IEnumerable<AgentPacketPartInfo> finalPacketParts, DataProcessingContext ctx)
        {

            var dict = new Dictionary<AgentPacketPartInfo, byte[]>();
            var packetPartToBytesDict = new Dictionary<PacketPartInfo, byte[]>();

            var chains = finalPacketParts.Select(x => ctx.PacketToChain.TryGetValue(x, out var chain) ? chain : null).Where(x => x != null);
            var packetPairsGroupByNode = chains.SelectMany(x => x).GroupBy(x => x.NodeDefinition);            

            // получаем байты всех частей
            foreach (var nodePacketPairsGroup in packetPairsGroupByNode)
            {
                if (this.dataNodeReadersPool.TryGetReader(nodePacketPairsGroup.Key, out var reader))
                {
                    var idToByteDict = reader.GetPacketPartInfoBytes(nodePacketPairsGroup.Select(x => x.PacketPartInfo.Id), ctx.CancellationToken);
                    foreach (var item in nodePacketPairsGroup)
                    {
                        if (idToByteDict.TryGetValue(item.PacketPartInfo.Id, out var bytes))
                        {
                            packetPartToBytesDict.Add(item.PacketPartInfo, bytes);
                        }
                    }
                }
                else
                {
                    // TODO instrumentation
                    this.InstrumentateNoReaderForDef(nodePacketPairsGroup.Key, "Не найден DataReader для получения бинарных данных записей для обработки");
                }
            }

            // собираем финальный словарь
            foreach (var finalPacketPart in finalPacketParts)
            {
                if (ctx.PacketToChain.TryGetValue(finalPacketPart, out var chain))
                {
                    var partBytes = new List<byte[]>(chain.Count);
                    foreach (var part in chain)
                    {
                        if (packetPartToBytesDict.TryGetValue(part.PacketPartInfo, out var bytes))
                        {
                            partBytes.Add(bytes);
                        }
                        else
                        {
                            partBytes = null;
                            break;
                        }
                    }

                    if (partBytes != null)
                    {
                        dict.Add(finalPacketPart, partBytes.SelectMany(x => x).ToArray());
                    }
                }
            }

            return dict; 
        }

        private void ProcessForProvider(string providerKey, IEnumerable<AgentPacketPartInfo> providerFinalPacketParts, DataProcessingContext ctx)
        {
            ctx.CancellationToken.ThrowIfCancellationRequested();

            if (this.processorContainers.TryGetValue(providerKey, out var processorContainers))
            {
                foreach (var packetPartBatch in providerFinalPacketParts.Batch(20))
                {
                    ctx.CancellationToken.ThrowIfCancellationRequested();
                    var computerPackets = new List<ComputerPacket>();
                    try
                    {
                        var packetToBytes = this.GetBytesForPacketChains(packetPartBatch, ctx);

                        foreach (var packetPart in packetPartBatch)
                        {
                            if (packetToBytes.TryGetValue(packetPart, out var packetBytes))
                            {
                                Stream stream = null;
                                try
                                {
                                    stream = new MemoryStream(packetBytes, false);
                                    computerPackets.Add(new ComputerPacket(packetPart.AgentInfo.ToComputerIdentity(), stream));
                                }
                                catch (Exception)
                                {
                                    stream?.Dispose();
                                    throw;
                                }
                            }
                            else
                            {
                                // TODO instrumentation
                            }
                        }

                        using (var packetProcessorsContext = new PacketProcessorsContext())
                        {
                            foreach (var processorContainer in processorContainers)
                            {
                                ctx.CancellationToken.ThrowIfCancellationRequested();

                                if (processorContainer.CanProcess)
                                {
                                    foreach (var computerPacket in computerPackets)
                                    {
                                        computerPacket.Stream.Seek(0, SeekOrigin.Begin);
                                    }

                                    try
                                    {
                                        processorContainer.Processor.Process(computerPackets, packetProcessorsContext, ctx.CancellationToken);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        ctx.CancellationToken.ThrowIfCancellationRequested();
                                    }
                                    catch (Exception)
                                    {
                                        // TODO instrumentation
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        foreach (var computerPacket in computerPackets)
                        {
                            computerPacket.Stream.Dispose();
                        }
                    }
                }
            }
            else
            {
                // TODO instrumentation
                this.InstrumentateNoProcessorsForProvider(providerKey, providerFinalPacketParts);
            }
        }

        private void InstrumentateNoReaderForDef(DataNodeDefinition definition, string message)
        {
            // TODO implement instrumentation
        }

        private void InstrumentateNoReaderForStorageToken(string token, string message)
        {
            // TODO implement instrumentation
        }

        private void InstrumentateNoProcessorsForProvider(string providerKey, IEnumerable<PacketPartInfo> packetParts)
        {
            // TODO implement instrumentation
        }

        private string GetRandomSqlConnectionAppName()
        {
            var connectionAppRandomPart = new Random(DateTime.UtcNow.Millisecond).Next(1, 9999).ToString("0000");
            return $";ApplicationName=DPU_{connectionAppRandomPart}";
        }
    }
}
