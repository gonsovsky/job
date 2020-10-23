using Atoll.Transport.Client.Bundle.Dto;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{

    public class TransferedPacketStats
    {

        public ITransportPacketInfo PacketInfo { get; set; }

        public SendStats SendStats { get; set; }

        public PacketProcessingResult Result { get; set; }

    }

}
