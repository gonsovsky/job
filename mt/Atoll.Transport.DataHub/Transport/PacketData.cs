using System;
using System.IO;

namespace Atoll.Transport.DataHub
{
    public class PacketIdData
    {
        public string PacketId { get; set; }
        public string ProviderKey { get; set; }
        public AgentIdentity AgentIdData { get; set; }
    }
}
