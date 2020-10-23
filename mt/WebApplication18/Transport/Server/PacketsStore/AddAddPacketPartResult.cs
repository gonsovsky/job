namespace WebApplication18.Transport
{
    public class AddAddPacketPartResult
    {
        public bool Success { get; }
        public AddPacketPartRequest Request { get;}
        //public string StorageToken { get; }

        protected AddAddPacketPartResult(AddPacketPartRequest request, bool success)
        {
            Request = request;
            Success = success;
        }

        public static AddAddPacketPartResult SuccessResult(AddPacketPartRequest request)
        {
            return new AddAddPacketPartResult(request, true);
        }

        public static AddAddPacketPartResult FailResult(AddPacketPartRequest request)
        {
            return new AddAddPacketPartResult(request, false);
        }
    }

    //public interface IPacketWriter: IDisposable
    //{

    //    void Write(byte[] buffer, int offset, int count);
    //    void SetLength(long length);
    //    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    //}
    
}
