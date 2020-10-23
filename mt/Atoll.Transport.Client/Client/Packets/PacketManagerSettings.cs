using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class PacketManagerSettings
    {
        public string PacketsDirectory { get; set; }
        public string PacketsTempDirectory { get; set; }

        public PacketManagerSettings(string packetsDirectory, string packetsTempDirectory)
        {
            this.PacketsDirectory = packetsDirectory;
            this.PacketsTempDirectory = packetsTempDirectory;
        }
    }
}
