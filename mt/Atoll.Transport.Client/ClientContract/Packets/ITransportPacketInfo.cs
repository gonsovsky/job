using System.Collections.Generic;
using System.IO;

namespace Atoll.Transport.Client.Contract
{

    /// <summary>
    /// 
    /// </summary>
    public interface ITransportPacketInfo
    {
        PacketIdentity Identity { get; }
        string ProviderKey { get; }
        int Length { get; }
        Stream GetReadOnlyStreamOrDefault();
    }
}
