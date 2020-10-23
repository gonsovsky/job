using System;
using System.IO;

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// <see cref="ITransportPacketInfo"/>
    /// </summary>
    public class TransportPacketInfo : ITransportPacketInfo
    {
        public string Id { get; }
        public string ProviderKey { get; }

        public int Length { get; }

        private readonly Func<Stream> streamFactory;

        public TransportPacketInfo(string providerKey, string id, int length, Func<Stream> streamFactory)
        {
            this.Id = id;
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
