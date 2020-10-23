using System.Collections.Generic;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class TransportResponseStats
    {
        public IDictionary<ITransportPacketInfo, SendStats> SendedPacketsStats { get; set; }

        public IList<TransferedPacketStats> TransferedPacketsProcessingResults { get; set; }

        public TransportResponseStats()
        {
            this.SendedPacketsStats = new Dictionary<ITransportPacketInfo, SendStats>();
            this.TransferedPacketsProcessingResults = new List<TransferedPacketStats>();
        }
    }
}
