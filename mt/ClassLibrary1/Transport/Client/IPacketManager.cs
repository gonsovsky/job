using System.Collections.Generic;
using System.IO;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    //public enum PacketProcessingResultResult
    //{
    //    NoProcessor = 0,
    //    Success = 1,
    //    Error = 2,
    //}

    /// <summary>
    /// сервис для управления пакетами
    /// сервис представляет собой основные методы которые будут иметь отличия в реализациях того где хранятся(и тп) пакеты
    /// </summary>
    public interface IPacketManager
    {
        PacketCreationResult Create(string providerKey);
        void Commit(string providerKey, IEnumerable<ITransportPacket> packets, CommitOptions options);
        void SavePacket(string providerKey, string packetId, Stream stream);
        IEnumerable<ITransportPacketInfo> GetTransportPacketInfos();
        void SaveSendStats(string providerKey, string packetId, SendStats stats);
        SendStats ReadSendStats(string providerKey, string packetId);
        void RemoveAll();
    }
}
