using System;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// результат создания пакета (пакет и ресурсы связанные с ним)
    /// </summary>
    public class PacketCreationResult
    {
        public ITransportPacket Packet { get; set; }
        public IDisposable[] Resources { get; set; }
    }
}
