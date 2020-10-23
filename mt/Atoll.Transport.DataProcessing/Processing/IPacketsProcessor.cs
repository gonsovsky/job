using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Atoll.Transport.DataProcessing
{
    public class ComputerPacket
    {
        public ComputerIdentity ComputerIdentity { get; private set; }
        public Stream Stream { get; private set; }

        public ComputerPacket(ComputerIdentity computerIdentity, Stream stream)
        {
            this.ComputerIdentity = computerIdentity;
            this.Stream = stream;
        }
    }

    public interface IPacketProcessorsContext
    {

    }

    public class PacketProcessorsContext : IPacketProcessorsContext, IDisposable
    {
        public void Dispose()
        {
        }
    }

    public interface IPacketsProcessor
    {
        ///// <summary>
        ///// Идентификатор процессора.
        ///// </summary>
        //string Id { get; }

        ///// <summary>
        ///// Контур события для которого обрабатывает процессор.
        ///// </summary>
        //string CircuitName { get; }

        /// <summary>
        /// 
        /// </summary>
        void Process(IEnumerable<ComputerPacket> computerPackets, IPacketProcessorsContext ctx, CancellationToken token);
    }

    public class ComputerIdentity
    {

        /// <summary>
        /// Имя компьютера.
        /// </summary>
        public readonly string ComputerName;

        /// <summary>
        /// Имя домена компьютера.
        /// </summary>
        public readonly string DomainName;

        /// <summary>
        /// Конструктор.
        /// </summary>
        /// <param name="computerName">имя компьютера.</param>
        /// <param name="domainName">имя домена компьютера.</param>
        public ComputerIdentity(string computerName, string domainName)
        {
            this.ComputerName = computerName;
            this.DomainName = domainName;
        }

        public string ToFullComputerName()
        {
            if (!string.IsNullOrEmpty(this.DomainName) /*&& this.DomainName != "."*/)
            {
                return string.Concat(this.DomainName, "\\", this.ComputerName);
            }

            return this.ComputerName;
        }
    }
}
