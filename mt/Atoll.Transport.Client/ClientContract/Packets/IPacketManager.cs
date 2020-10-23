using System.Collections.Generic;
using System.IO;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// сервис для управления пакетами
    /// сервис представляет собой основные методы которые будут иметь отличия в реализациях того где хранятся(и тп) пакеты
    /// </summary>
    public interface IPacketManager
    {
        PacketCreationResult Create(string providerKey);
        void Commit(string providerKey, IEnumerable<ITransportPacket> packets, CommitOptions options);
        //void SavePacket(string providerKey, string packetId, Stream stream);
        //IEnumerable<ITransportPacketInfo> GetTransportPacketInfos();
        IEnumerable<ITransportProviderInfo> GetTransportProviderInfos();
        void SaveSendStats(string providerKey, PacketIdentity packetId, SendStats stats);
        SendStats ReadSendStats(string providerKey, PacketIdentity packetId);
        void RemoveAll();
        void ReInitOrderCounter();
    }

}
