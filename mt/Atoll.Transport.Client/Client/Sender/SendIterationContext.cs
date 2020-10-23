using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class SendIterationContext
    {
        public bool SendPackets { get; set; }
        public bool RequestConfigurations{ get; set; }
        //public int MessageSize { get; set; }

        public int Attempt { get; set; }
        public DateTime? FirstFailTimeUtc { get; set; }

        public TransportSettings TransportSettings { get; set; }
    }
}
