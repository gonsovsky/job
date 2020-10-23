using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
#if NETSTANDARD2_0
using System.Net.Http;
#endif
using System.Threading.Tasks;

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// сервис для пакетной передачи данных на сервер
    /// </summary>
    public interface ITransportClient
    {
        /// <summary>
        /// создать сессию записи пакетных данных
        /// </summary>
        /// <param name="providerKey">ключ провайдера данных</param>
        ITransportSession CreateSession(string providerKey);

        ITransportDataWriter CreateWriter(string providerKey, CommitOptions commitOptions = CommitOptions.DeletePrevious);
    }

    public interface ITransportDataWriter: IDisposable
    {
        long Length { get; }
        void Write(byte[] bytes);
    }

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
