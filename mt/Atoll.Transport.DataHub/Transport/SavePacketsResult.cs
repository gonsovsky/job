using Atoll.Transport;
using Atoll.Transport.Contract;
using System.Collections.Generic;

namespace Atoll.Transport.DataHub
{
    public class ParseBodyAndSavePacketsResult
    {
        public IList<TransferedPacketResponse> TransferedPackets { get; set; }
        public IReadOnlyCollection<ConfigurationRequestDataItem> ConfigurationsStats { get; set; }
    }
}
