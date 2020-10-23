using System;
using System.Collections.Generic;

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// <see cref="ITransportSession"/>
    /// </summary>
    public class TransportSession : ITransportSession
    {
        private readonly string providerKey;
        private readonly TransportSettings settings;
        private readonly IPacketManager packetManager;
        private List<ITransportPacket> packets;
        private List<IDisposable> resources;

        public TransportSession(string providerKey, IPacketManager packetManager, TransportSettings settings)
        {
            this.providerKey = providerKey;
            this.packetManager = packetManager;
            this.settings = settings;
            this.packets = new List<ITransportPacket>();
            this.resources = new List<IDisposable>();
        }

        public void Dispose()
        {
            this.ClearResources();
        }

        private void ClearResources()
        {
            foreach (var packet in packets)
            {
                if (packet is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            foreach (var resource in resources)
            {
                resource.Dispose();
            }

            this.resources.Clear();
        }

        public void Commit(CommitOptions options = CommitOptions.None)
        {
            this.Save(this.packets, options);
            this.packets.Clear();
            this.ClearResources();
        }

        public ITransportPacket CreatePacket()
        {
            PacketCreationResult createResult = null;
            try
            {
                createResult = this.packetManager.Create(this.providerKey);
                this.resources.AddRange(createResult.Resources);
            }
            catch (Exception)
            {
                if (createResult != null)
                {
                    foreach (var item in createResult.Resources)
                    {
                        item.Dispose();
                    }
                }

                throw;
            }

            return createResult.Packet;
        }

        public void Add(ITransportPacket packet)
        {
            this.packets.Add(packet);
        }

        public void Add(IEnumerable<ITransportPacket> packets)
        {
            this.packets.AddRange(packets);
        }

        protected virtual void Save(IEnumerable<ITransportPacket> packets, CommitOptions options)
        {
            this.packetManager.Commit(this.providerKey, packets, options);
        }
    }
}
