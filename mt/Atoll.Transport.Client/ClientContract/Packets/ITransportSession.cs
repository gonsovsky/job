using System;
using System.Collections.Generic;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// Сессия записи пакетных данных
    /// </summary>
    public interface ITransportSession : IDisposable
    {
        ITransportPacket CreatePacket();
        void Add(ITransportPacket packet);
        void Add(IEnumerable<ITransportPacket> packets);
        void Commit(CommitOptions options = CommitOptions.DeletePrevious);
    }
}
