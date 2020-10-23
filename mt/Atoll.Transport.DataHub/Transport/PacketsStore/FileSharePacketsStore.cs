//using System.Collections.Generic;
//using System.IO;
//using System;
//using System.Threading.Tasks;
//using ClassLibrary1.Transport;

//namespace Atoll.Transport.DataHub
//{

//    public class FileSharePacketsStore : IPacketsStore
//    {
//        private readonly PacketsStoreSettings settings;

//        public FileSharePacketsStore(PacketsStoreSettings settings)
//        {
//            this.settings = settings;
//        }

//        private string GetPacketPath(AgentIdentifierData agentId, string providerKey, string packet)
//        {
//            return Path.Combine(settings.PacketsDirectory, agentId.Domain, agentId.ComputerName, providerKey, packet.ToString());
//        }

//        private string GetPacketPath(PacketIdData packetIdData)
//        {
//            var agentId = packetIdData.AgentIdData;
//            return Path.Combine(settings.PacketsDirectory, agentId.Domain, agentId.ComputerName, packetIdData.ProviderKey, packetIdData.PacketId.ToString());
//        }

//        public Stream GetPacketWriteStream(AgentIdentifierData agentId, string providerKey, string packet)
//        {
//            var filePath = this.GetPacketPath(agentId, providerKey, packet);
//            try
//            {
//                return File.OpenWrite(filePath);
//            }
//            catch (DirectoryNotFoundException ex)
//            {
//                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
//                return File.OpenWrite(filePath);
//            }
//        }

//        protected Stream GetPacketReadStream(AgentIdentifierData agentId, string providerKey, string packet)
//        {
//            return File.OpenRead(this.GetPacketPath(agentId, providerKey, packet));
//        }

//        public Func<Stream> GetPacketReadStreamFactory(AgentIdentifierData agentId, string providerKey, string packet)
//        {
//            return () => GetPacketReadStream(agentId, providerKey, packet);
//        }

//        public PacketStats GetPacketStats(AgentIdentifierData agentId, string providerKey, string packet)
//        {
//            var fi = new FileInfo(this.GetPacketPath(agentId, providerKey, packet));
//            return new PacketStats()
//            {
//                CurrentSize = fi.Exists ? fi.Length : 0,
//            };
//        }

//        public async Task DeletePackets(List<PacketIdData> list)
//        {
//            foreach (var item in list)
//            {
//                if (!FileSystemHelper.SafeDeleteFile(this.GetPacketPath(item)))
//                {
//                    // TODO instrumentation
//                }
//            }
//        }

//        public async Task AddPacketPartAsync(AgentIdentifierData agentId, AddPacketPartRequest request)
//        {
//            using (var stream = this.GetPacketWriteStream(agentId, request.ProviderKey, request.PacketId))
//            {
//                await stream.WriteAsync(request.Bytes, 0, request.Bytes.Length);
//            }
//        }

//        public async Task AddPacketPartsAsync(AgentIdentifierData agentId, IList<AddPacketPartRequest> requests)
//        {
//            foreach (var request in requests)
//            {
//                await this.AddPacketPartAsync(agentId, request);
//            }
//        }
//    }

//}
