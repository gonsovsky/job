using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class TransportDataWriter: ITransportDataWriter
    {
        private ITransportPacket packet;
        private readonly ITransportSession session;
        private readonly CommitOptions options;

        public TransportDataWriter(ITransportSession session, CommitOptions options)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.options = options;
        }

        private void InitPacket()
        {
            if (this.packet == null)
            {
                this.packet = this.session.CreatePacket();
            }
        }

        public long Length => this.packet?.Length ?? 0;

        public void Dispose()
        {
            if (this.packet != null)
            {
                this.session.Add(this.packet);
                this.session.Commit(this.options);
            }

            this.session.Dispose();
        }

        public void Write(byte[] bytes)
        {
            this.InitPacket();
            this.packet.Write(bytes);
        }
    }
}
