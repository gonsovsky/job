using System;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
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
