using System.Collections.Generic;
using Atoll.Transport.Client.Contract;
using Atoll.Transport.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class TransportSendStats
    {
        public IList<ITransportPacketInfo> IgnoredPackets { get; set; }
        public IDictionary<ITransportPacketInfo, SendStats> SendedPacketsStats { get; set; }
        public IList<ConfigurationRequestDataItem> ConfigurationsInfos { get; set; }

        public TransportSendStats()
        {
            this.IgnoredPackets = new List<ITransportPacketInfo>();
            this.SendedPacketsStats = new Dictionary<ITransportPacketInfo, SendStats>();
        }
    }
}
