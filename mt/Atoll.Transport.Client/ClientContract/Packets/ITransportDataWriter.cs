using System;

namespace Atoll.Transport.Client.Contract
{
    public interface ITransportDataWriter: IDisposable
    {
        long Length { get; }
        void Write(byte[] bytes);
    }
}
