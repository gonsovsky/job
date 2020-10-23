using System;

namespace Atoll.Transport.DataHub
{
    public class AddAddPacketPartResult
    {
        public bool Success { get; }
        public AddPacketPartRequest Request { get;}
        public string StorageToken { get; }
        public string Id { get; }

        protected AddAddPacketPartResult(AddPacketPartRequest request, bool success, string storageToken, string id)
        {
            Request = request;
            Success = success;
            StorageToken = storageToken ?? throw new ArgumentNullException("storageToken");
            Id = id ?? throw new ArgumentNullException("id");
        }

        public static AddAddPacketPartResult SuccessResult(AddPacketPartRequest request, string storageToken, string id)
        {
            return new AddAddPacketPartResult(request, true, storageToken, id);
        }

        public static AddAddPacketPartResult FailResult(AddPacketPartRequest request)
        {
            return new AddAddPacketPartResult(request, false, null, null);
        }
    }

    //public interface IPacketWriter: IDisposable
    //{

    //    void Write(byte[] buffer, int offset, int count);
    //    void SetLength(long length);
    //    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    //}
    
}
