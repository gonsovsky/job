using Atoll.Transport;
using System.Collections.Generic;

namespace WebApplication18.Transport
{
    public class ParseBodyAndSavePacketsResult
    {
        public IList<TransferedPacketResponse> TransferedPackets { get; set; }
        public IReadOnlyCollection<ConfigurationRequestDataItem> ConfigurationsStats { get; set; }
    }
}
