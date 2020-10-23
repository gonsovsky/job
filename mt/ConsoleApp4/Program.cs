using Atoll.Transport;
using Atoll.Transport.Client.Bundle;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class App { }
    public class FileInfo
    {
        public string FileName { get; set; }
        public long Size { get; set; }
    }
    public class ComputerInfo { }

    public class TransportBootstrap
    {
        public ITransportClient transportClient;
        public ITransportSenderWorker senderWorker;

        public TransportBootstrap Init(string tempDir)
        {
            if (!FileSystemHelper.SafeDeleteDirectory(tempDir))
            {
                Debug.WriteLine("failed delete temp dir");
            }

            var pcktsDir = Path.Combine(tempDir, "pckts");
            var tempPcktsDir = Path.Combine(tempDir, "tmp_pckts");
            var packetManager = new PacketManager(new PacketManagerSettings(pcktsDir, tempPcktsDir));
            var transportSettings = new TransportSettings()
            {
                PacketSizeLimits = new SendMessageSizeLimits
                {
                    Min = 0,
                    Max = TransportConstants.DefaultMaxClientPacketSize,
                }
            };
            string url = "http://localhost:5002/dhu/transport/exchange";
            this.transportClient = new TransportClient(packetManager, transportSettings);
            var agentInfoService = new TransportAgentInfoService(new RealComputerIdentityProvider());
            var cnfsDir = Path.Combine(tempDir, "conf");
            var tempcnfsDir = Path.Combine(tempDir, "conf_pckts");
            var confStore = new ConfigurationStore(cnfsDir, tempcnfsDir);
            confStore.Subscribe(new TestOnConfigUpdate());
            this.senderWorker = new TransportSenderWorker(packetManager, agentInfoService, confStore, url, transportSettings, new SendStateStore());

            return this;
        }
    }

    class TestOnConfigUpdate : IConfigurationUpdateSubscriber
    {
        public string ProviderKey => "test";

        public void OnUpdate(Func<Stream> configStreamFactory)
        {
            Console.WriteLine("On config update");
            Console.WriteLine("ProviderKey: " + ProviderKey);
            Console.WriteLine("Config -");
            using (var stream = configStreamFactory())
            using (var streamReader = new StreamReader(stream))
            {
                Console.WriteLine(streamReader.ReadToEnd());
            }
        }
    }

    class Program
    {
        private static List<App> apps;
        private static List<FileInfo> files;
        private static List<ComputerInfo> computerInfos;
        private static TransportBootstrap transportBootstrap;

        static void Main(string[] args)
        {
            Console.WriteLine("type inv, dinv, rqcnf, test1, qtest1");

            using (CancellationTokenSource cs = new CancellationTokenSource())
            {

                CancellationTokenSource test1Cts = null;
                Task test1Task = null;

                try
                {
                    Init(cs.Token);
                    string cmd;
                    do
                    {
                        cmd = Console.ReadLine();
                        if (cmd == "inv")
                        {
                            RunProvider(cs.Token, false);
                            Console.WriteLine("additional inv");
                        }
                        if (cmd == "dinv")
                        {
                            RunProvider(cs.Token, true);
                            Console.WriteLine("replace inv");
                        }
                        if (cmd == "rqcnf")
                        {
                            RunConfigUpdate(cs.Token);
                            Console.WriteLine("request configs");
                        }
                        if (cmd == "test1")
                        {
                            test1Cts = CancellationTokenSource.CreateLinkedTokenSource(cs.Token);
                            test1Task = Task.Factory.StartNew(async () => await StartTest1(TimeSpan.FromMilliseconds(30), test1Cts.Token), TaskCreationOptions.LongRunning);
                            Console.WriteLine("start test1");
                        }
                        if (cmd == "qtest1")
                        {
                            try
                            {
                                test1Cts?.Cancel();
                                test1Task?.Wait();
                            }
                            finally
                            {
                                test1Cts?.Dispose();
                            }

                            Console.WriteLine("stop test1");
                        }
                    } while (cmd.Trim() != "q");

                    cs.Cancel();
                    test1Task?.Wait();
                }
                finally
                {
                    test1Cts?.Dispose();
                }
            }
        }

        private static async Task StartTest1(TimeSpan tryTimeout, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                RunProvider(token, false);
                await Task.Delay(tryTimeout);
            }
        }

        static void Init(CancellationToken token)
        {
            apps = new List<App>();
            files = new List<FileInfo>()
            {
                new FileInfo
                {
                    FileName = "test1.tmp",
                    Size = 11,
                },
                new FileInfo
                {
                    FileName = "test2.tmp",
                    Size = 12,
                }
            };
            computerInfos = new List<ComputerInfo>();
            transportBootstrap = new TransportBootstrap().Init(Path.Combine(Directory.GetCurrentDirectory(), "temp2"));
            StartSendingJob(token);
        }

        static void StartSendingJob(CancellationToken token)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(y =>
            {
                try
                {
                    RunSending(token);

                    if (token.IsCancellationRequested)
                    {
                        timer.Dispose();
                    }
                }
                catch (Exception ex)
                {

                }
            }), null, 3000, 3000);
        }

        static void RunSending(CancellationToken token)
        {
            if (!token.IsCancellationRequested)
            {
                transportBootstrap.senderWorker.Process();
            }
        }

        static void RunProvider(CancellationToken token, bool delPrevious)
        {
            var transportClient = transportBootstrap.transportClient;

            if (!token.IsCancellationRequested)
            {
                using (var writer = transportClient.CreateWriter("testProvider", delPrevious ? CommitOptions.DeletePrevious : CommitOptions.None))
                {
                    var str = JsonConvert.SerializeObject(files);
                    var bytes = Encoding.UTF8.GetBytes(str);
                    writer.Write(bytes);
                }
            }
        }

        static void RunConfigUpdate(CancellationToken token)
        {
            var transportClient = transportBootstrap.transportClient;

            if (!token.IsCancellationRequested)
            {
                transportBootstrap.senderWorker.Process();
            }
        }
    }
}
