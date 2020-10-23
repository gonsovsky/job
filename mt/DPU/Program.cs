using Atoll.Transport;
using Atoll.Transport.DataProcessing;
using Atoll.Transport.ServerBundle;
using Coral.Atoll.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DPU
{
    public class ProcessorContainerFactory : IProcessorContainerFactory
    {
        private readonly IProcessorManager manager;

        public string Id => manager.Id;

        public string CircuitName => manager.CircuitName;

        public ProcessorContainerFactory(IProcessorManager manager)
        {
            this.manager = manager;
        }

        public ProcessorContainer GetProcessorContainer()
        {
            return new ProcessorContainer(new TestPacketsProcessor());
        }
    }

    public class TestPacketsProcessor : IPacketsProcessor
    {
        private int counter;
        private bool first = true;
        public void Process(ComputerIdentity computerIdentity, Stream stream, CancellationToken token)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                var val = serializer.Deserialize(jsonTextReader);
                if (first)
                {
                    this.first = false;
                    Console.WriteLine($"processed 1 event");
                }
                var count = Interlocked.Increment(ref this.counter);
                if (count % 200 == 0)
                {
                    Console.WriteLine($"processed another 200 events");
                }

                if (count % 1000 == 0)
                {
                    Console.WriteLine($"processed {count} events");
                }
                //Console.WriteLine($"For computer - {computerIdentity.ToFullComputerName()} deserialized value - {val?.ToString()}");
            }
        }

        public void Process(IEnumerable<ComputerPacket> computerPackets, IPacketProcessorsContext ctx, CancellationToken token)
        {
            foreach (var item in computerPackets)
            {
                this.Process(item.ComputerIdentity, item.Stream, token);
            }
        }
    }

    public class TestProcessorManager : IProcessorManager
    {
        public string Id => "testProvider";

        public string CircuitName => "AtollDefault";

        public bool IsEnabled => true;

        public IProcessorContainerFactory GetProcessorContainerFactory()
        {
            return new ProcessorContainerFactory(this);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            var taskReadConsole = Task.Factory.StartNew(() =>
            {
                while (Console.ReadLine() != "X")
                {
                }
            }, TaskCreationOptions.LongRunning);


            var mongoUrls = new List<string>()
            {
                "mongodb://192.168.100.184:27017",
               // "mongodb://localhost:5555",
            };

            var mongoDefs = mongoUrls.Select(x => new DataNodeDefinition(new Uri(x))).ToArray();

            ProcessingService service = null;
            try
            {
                service = new ProcessingService(new ComputerNameAndProcessIdUnitIdProvider(TimeSpan.FromSeconds(30)), new List<IProcessorManager>()
                {
                    new TestProcessorManager(),
                }, new ProcessingSettings
                {
                    DataNodes = mongoDefs,
                    PcdConnectionString = "test",
                    JobLeaseLifeTime = TimeSpan.FromMinutes(20),
                    DeleteProcessedRecordsTimeout = Debugger.IsAttached ? (TimeSpan?)TimeSpan.FromSeconds(12) : null,
                    ReservationRangeLimits = new ReservationRangeLimits
                    {
                        Min = 200,
                        Max = 300
                    },
                    LoadedInMemoryBinaryPartsBatchSize = 20,
                    PacketsLifeTime = TimeSpan.FromDays(7),
                });

                service.OnCreate();
                service.OnStart();
                taskReadConsole.Wait();
            }
            finally
            {
                service?.OnStop();
            }
        }
    }
}
