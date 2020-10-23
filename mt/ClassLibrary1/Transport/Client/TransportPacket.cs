using System;
using System.IO;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// <see cref="ITransportPacket"/>
    /// </summary>
    public class TransportPacket : ITransportPacket
    {
        private readonly Stream stream;
        private readonly Action saveDataAction;

        public long Length { get; private set; }

        public string Id { get; private set; }

        public TransportPacket(string id, Stream stream, Action saveDataAction, int? length = null)
        {
            this.Id = id;
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
