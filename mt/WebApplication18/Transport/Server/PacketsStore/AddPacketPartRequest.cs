namespace WebApplication18.Transport
{

    public class AddPacketPartRequest
    {
        public string ProviderKey { get; set; }
        public string PacketId { get; set; }
        public bool IsFinal { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public byte[] Bytes { get; set; }
    }
    
}
