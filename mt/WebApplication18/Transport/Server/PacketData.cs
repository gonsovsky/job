using System;
using System.IO;

namespace WebApplication18.Transport
{
    public class PacketIdData
    {
        public string PacketId { get; set; }
        public string ProviderKey { get; set; }
        public AgentIdentifierData AgentIdData { get; set; }
    }
}
