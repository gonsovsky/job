using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// <see cref="ITransportClient"/>
    /// </summary>
    public class TransportClient : ITransportClient
    {
        private readonly IPacketManager packetManager;
        private readonly ITransportAgentInfoService transportAgentInfoService;

        public TransportClient(ITransportAgentInfoService transportAgentInfoService, IPacketManager packetManager)
        {
            this.packetManager = packetManager ?? throw new System.ArgumentNullException(nameof(packetManager));
            this.transportAgentInfoService = transportAgentInfoService ?? throw new System.ArgumentNullException(nameof(transportAgentInfoService));
        }

        /// <inheritdoc />
        public ITransportSession CreateSession(string providerKey)
        {
            return new TransportSession(providerKey, this.packetManager);
        }

        /// <inheritdoc />
        public ITransportDataWriter CreateWriter(string providerKey, CommitOptions commitOptions = CommitOptions.DeletePrevious)
        {
            return new TransportDataWriter(this.CreateSession(providerKey), commitOptions);
        }
    }
}
