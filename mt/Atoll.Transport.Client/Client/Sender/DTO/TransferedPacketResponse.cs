namespace Atoll.Transport.Client.Bundle.Dto
{
    public class TransferedPacketResponse
    {
        public string PacketId { get; set; }
        public string ProviderKey { get; set; }

        public PacketProcessingResult Result { get; set; }

        public string StorageToken { get; set; }
        public string Id { get; set; }
    }
}
