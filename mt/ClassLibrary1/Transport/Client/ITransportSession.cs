using System;
using System.Collections.Generic;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
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
