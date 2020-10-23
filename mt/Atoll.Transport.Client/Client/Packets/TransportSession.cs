using System;
using System.Collections.Generic;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// <see cref="ITransportSession"/>
    /// </summary>
    public class TransportSession : ITransportSession
    {
        private readonly string providerKey;
        private readonly IPacketManager packetManager;
        private List<ITransportPacket> packets;
        private List<IDisposable> resources;

        public TransportSession(string providerKey, IPacketManager packetManager)
        {
            this.providerKey = providerKey;
            this.packetManager = packetManager;
            this.packets = new List<ITransportPacket>();
            this.resources = new List<IDisposable>();
        }

        public void Dispose()
        {
            this.ClearResources();
        }

        private void ClearResources()
        {
            foreach (var packet in this.packets)
            {
                if (packet is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            this.packets.Clear();

            foreach (var resource in this.resources)
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
