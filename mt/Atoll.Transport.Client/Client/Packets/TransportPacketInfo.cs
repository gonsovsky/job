using System;
using System.IO;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// <see cref="ITransportPacketInfo"/>
    /// </summary>
    public class TransportPacketInfo : ITransportPacketInfo
    {
        public PacketIdentity Identity { get; }
        public string ProviderKey { get; }

        public int Length { get; }

        private readonly Func<Stream> streamFactory;

        public TransportPacketInfo(string providerKey, PacketIdentity identity, int length, Func<Stream> streamFactory)
        {
            this.Identity = identity;
            this.ProviderKey = providerKey;
            this.Length = length;
            this.streamFactory = streamFactory;
        }

        public Stream GetReadOnlyStreamOrDefault()
        {
            return new ReadOnlyStream(this.streamFactory());
        }
    }
}
