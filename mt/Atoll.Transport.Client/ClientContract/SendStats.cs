namespace Atoll.Transport.Client.Contract
{
    public class SendStats
    {
        public int TransferedBytes { get; set; }
        public bool TransferCompleted { get; set; }

        public PacketPartIdentity PreviousPartIdentity { get; set; }
    }

    public class PacketPartIdentity
    {
        public string StorageToken { get; set; }
        public string Id { get; set; }

        public PacketPartIdentity(string storageToken, string id)
        {
            StorageToken = storageToken;
            Id = id;
        }
    }
}
