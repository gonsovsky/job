using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atoll.Transport.Client.Bundle;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;
using Newtonsoft.Json;

namespace D_Agent
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
        private PacketManager packetManager;

        public TransportBootstrap Init(string tempDir)
        {
            if (!FileSystemHelper.SafeDeleteDirectory(tempDir))
            {
                Debug.WriteLine("failed delete temp dir");
            }

            var pcktsDir = Path.Combine(tempDir, "pckts");
            var tempPcktsDir = Path.Combine(tempDir, "tmp_pckts");
            this.packetManager = new PacketManager(new PacketManagerSettings(pcktsDir, tempPcktsDir));
            var transportSettings = new TransportSettings()
            {
                PacketSizeLimits = new SendMessageSizeLimits
                {
                    Min = 0,
                    Max = 1 * 1024 * 1024,
                    //Max = 100,
                }
            };
            string url = "http://192.168.100.184:5001/dhu/transport/exchange";
            var agentInfoService = new TransportAgentInfoService(new RealComputerIdentityProvider());
            this.transportClient = new TransportClient(agentInfoService, packetManager);           
            var cnfsDir = Path.Combine(tempDir, "conf");
            var tempcnfsDir = Path.Combine(tempDir, "conf_pckts");
            var confStore = new ConfigurationStore(cnfsDir, tempcnfsDir);
            confStore.Subscribe(new TestOnConfigUpdate());
            this.senderWorker = new TransportSenderWorker(packetManager, agentInfoService, confStore, url, transportSettings, new SendStateStore());

            return this;
        }

        public void Dispose()
        {
            this.packetManager?.Dispose();
        }
    }

    class TestOnConfigUpdate : IConfigurationUpdateSubscriber
    {
        public string ProviderKey => "test";

        public void OnUpdate(Func<Stream> configStreamFactory)
        {
            Console.WriteLine("On config update");
            Console.WriteLine("ProviderKey: "+ ProviderKey);
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
        private static List<FileInfo> filesLong;
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
                            if (test1Task == null)
                            {
                                test1Cts = CancellationTokenSource.CreateLinkedTokenSource(cs.Token);
                                test1Task = StartTest1(TimeSpan.FromMilliseconds(100), test1Cts.Token, 100);
                                Console.WriteLine("start test1");
                            }
                        }
                        if (cmd == "test1Load")
                        {
                            if (test1Task == null)
                            {
                                test1Cts = CancellationTokenSource.CreateLinkedTokenSource(cs.Token);
                                test1Task = StartTest1(null, test1Cts.Token);
                                Console.WriteLine("start test1");
                            }
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
                                test1Cts = null;
                                test1Task = null;
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
                transportBootstrap?.Dispose();
            }
        }

        private static Task Delay(TimeSpan timeSpan)
        {
            var tcs = new TaskCompletionSource<bool>();
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += (obj, args) =>
            {
                tcs.TrySetResult(true);
            };
            timer.Interval = timeSpan.TotalMilliseconds;
            timer.AutoReset = false;
            timer.Start();
            return tcs.Task;
        }

        private static Task StartTest1(TimeSpan? tryTimeout, CancellationToken token, int count = 1)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    for (int i = 0; i < count; i++)
                    {
                        RunProvider(token, false);
                    }
                    if (tryTimeout != null)
                    {
                        Delay(tryTimeout.Value).Wait();
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        private static Task StartTest2(TimeSpan? tryTimeout, CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    RunProvider(token, false);
                    if (tryTimeout != null)
                    {
                        Delay(tryTimeout.Value).Wait();
                    }
                }
            }, TaskCreationOptions.LongRunning);
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

            filesLong = Enumerable.Range(0, 300).Select(x =>
            {
                return new FileInfo
                {
                    FileName = x + "_test.tmp",
                    Size = x,
                };
            }).ToList();

            computerInfos = new List<ComputerInfo>();
            var dir = !Debugger.IsAttached
                ? ("temp_" + Process.GetCurrentProcess().Id)
                : "temp4";

            transportBootstrap = new TransportBootstrap()
                .Init(Path.Combine(Directory.GetCurrentDirectory(), dir));

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
                catch(Exception ex)
                {

                }
            }), null, 10, 10);
        }

        static void RunSending(CancellationToken token)
        {
            if (!token.IsCancellationRequested)
            {
                transportBootstrap.senderWorker.Process();
            }
        }

        private static long counter;
        static void RunProvider(CancellationToken token, bool delPrevious)
        {
            var transportClient = transportBootstrap.transportClient;
            
            if (!token.IsCancellationRequested)
            {
                using (var writer = transportClient.CreateWriter("testProvider", delPrevious ? CommitOptions.DeletePrevious : CommitOptions.None))
                {
                    //var str = Interlocked.Increment(ref counter) % 2 == 0 
                    //    ? JsonConvert.SerializeObject(files) 
                    //    : JsonConvert.SerializeObject(filesLong);

                    var str = JsonConvert.SerializeObject(filesLong, Formatting.None);

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
