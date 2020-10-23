using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Atoll.Transport.Client.Bundle.Dto
{

    public class TransportResponse
    {

        public string ErrorMessage { get; private set; }
        public StaticConfigDataResponse StaticConfigData { get; set; }
        public DbTokenDataResponse DbTokenData { get; set; }

        public IList<TransferedPacketResponse> TransferedPackets { get; set; }
        public IList<ConfigurationResponse> Configurations { get; set; }

    }

}