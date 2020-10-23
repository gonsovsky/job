namespace Atoll.Transport.DataHub
{

    public class AddPacketPartRequest
    {
        public string ProviderKey { get; set; }
        public string PacketId { get; set; }
        public bool IsFinal { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public byte[] Bytes { get; set; }

        public string PreviousPartStorageToken { get; set; }
        public string PreviousPartId { get; set; }
    }
    
}
