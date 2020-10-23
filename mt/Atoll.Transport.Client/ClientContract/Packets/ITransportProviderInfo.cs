using System.Collections.Generic;

namespace Atoll.Transport.Client.Contract
{
    public interface ITransportProviderInfo
    {
        string ProviderKey { get; }
        IEnumerable<ITransportPacketInfo> GetPackets();
    }
}
