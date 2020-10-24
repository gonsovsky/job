using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using Atoll.Transport.Client.Bundle;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;
using Newtonsoft.Json;

namespace TransportMongo
{
    public class TransportMongo
    {
        public ITransportClient TransportClient;
        public ITransportSenderWorker SenderWorker;
        private readonly PacketManager _packetManager;

        public event Action<int> OnSent;

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
        }

        public void Dispose()
        {
            this._packetManager?.Dispose();
        }

        public void SendMessage(int itemId, string queueName, Stream data)
        {
            using (var writer = TransportClient.CreateWriter(queueName, CommitOptions.None))
            {
                writer.Write(ReadFully(data));
            }
            this.SenderWorker.Process();
            OnSent?.Invoke(itemId);
        }

        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
