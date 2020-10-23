using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Atoll.Transport.Client.Bundle;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;

namespace TransportMongo
{
    public class TransportMongo
    {
        public ITransportClient TransportClient;
        public ITransportSenderWorker SenderWorker;
        private readonly PacketManager _packetManager;

        public TransportMongo(string tempDir, string url)
        {
            if (!FileSystemHelper.SafeDeleteDirectory(tempDir))
            {
                Debug.WriteLine("failed delete temp dir");
            }

            var pcktsDir = Path.Combine(tempDir, "pckts");
            var tempPcktsDir = Path.Combine(tempDir, "tmp_pckts");
            this._packetManager = new PacketManager(new PacketManagerSettings(pcktsDir, tempPcktsDir));
            var transportSettings = new TransportSettings()
            {
                PacketSizeLimits = new SendMessageSizeLimits
                {
                    Min = 0,
                    Max = 1 * 1024 * 1024,
                    //Max = 100,
                }
            };
            var agentInfoService = new TransportAgentInfoService(new RealComputerIdentityProvider());
            this.TransportClient = new TransportClient(agentInfoService, _packetManager);
            var cnfsDir = Path.Combine(tempDir, "conf");
            var tempcnfsDir = Path.Combine(tempDir, "conf_pckts");
            var confStore = new ConfigurationStore(cnfsDir, tempcnfsDir);
            confStore.Subscribe(new TestOnConfigUpdate());
            this.SenderWorker = new TransportSenderWorker(_packetManager, agentInfoService, confStore, url, transportSettings, new SendStateStore());
            CancellationTokenSource cs = new CancellationTokenSource();
            this.SenderWorker.Process();
        }

        public void Dispose()
        {
            this._packetManager?.Dispose();
        }
    }
}
