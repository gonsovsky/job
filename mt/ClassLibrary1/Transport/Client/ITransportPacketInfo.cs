using System.IO;
#if NETSTANDARD2_0
#endif

namespace ClassLibrary1.Transport
{
    public interface ITransportPacketInfo
    {
        string Id { get; }
        string ProviderKey { get; }
        int Length { get; }
        Stream GetReadOnlyStreamOrDefault();
    }
}
