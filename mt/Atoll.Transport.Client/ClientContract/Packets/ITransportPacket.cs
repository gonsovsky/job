
namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// пакет(/файл) данных который будет доставлен на сервер и обработан как единое целое
    /// </summary>
    public interface ITransportPacket
    {
        PacketIdentity Identity { get; }
        long Length { get; }
        void Write(byte[] bytes);

        /// <summary>
        /// сохраняет пакет
        /// </summary>
        /// <remarks>после сохранения в пакет как правило нельзя записывать данные</remarks>
        void Save(string providerKey, IPacketManager packetManager);
    }
}
