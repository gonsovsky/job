using System;
using System.IO;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// <see cref="ITransportPacket"/>
    /// </summary>
    public class TransportPacket : ITransportPacket
    {
        private readonly Stream stream;
        private readonly Action saveDataAction;

        public long Length { get; private set; }

        public PacketIdentity Identity { get; private set; }

        public TransportPacket(PacketIdentity id, Stream stream, Action saveDataAction, int? length = null)
        {
            this.Identity = id;
            this.stream = stream;
            this.saveDataAction = saveDataAction;

            if (length != null)
            {
                this.Length = length.Value;
            }
            else if (stream.CanSeek)
            {
                this.Length = this.stream.Length;
            }
        }

        public void Write(byte[] bytes)
        {
            this.stream.Write(bytes, 0, bytes.Length);
            this.Length += bytes.Length;
        }

        public virtual void Save(string providerKey, IPacketManager packetManager)
        {
            this.stream.Flush();
            this.saveDataAction();
        }
    }
}
