#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// <see cref="ITransportClient"/>
    /// </summary>
    public class TransportClient : ITransportClient
    {
        private readonly IPacketManager packetManager;
        private readonly TransportSettings settings;

        public TransportClient(IPacketManager packetManager, TransportSettings settings)
        {
            this.packetManager = packetManager;
            this.settings = settings;
        }

        /// <inheritdoc />
        public ITransportSession CreateSession(string providerKey)
        {
            return new TransportSession(providerKey, this.packetManager, this.settings);
        }

        public ITransportDataWriter CreateWriter(string providerKey, CommitOptions commitOptions = CommitOptions.DeletePrevious)
        {
            return new TransportDataWriter(this.CreateSession(providerKey), commitOptions);
        }
    }
}
