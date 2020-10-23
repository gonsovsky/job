using System.Collections.Generic;
using System.Linq;

namespace Atoll.Transport.DataHub
{
    public class AddAddPacketsPartsResult
    {
        public bool Success { get; }
        public IEnumerable<AddAddPacketPartResult> Results { get; }
        public string StorageToken { get; }

        protected AddAddPacketsPartsResult(string storageToken, IEnumerable<AddAddPacketPartResult> results, bool success)
        {
            Results = results;
            Success = success;
        }

        public static AddAddPacketsPartsResult CreateResult(string storageToken, IEnumerable<AddAddPacketPartResult> results)
        {
            return new AddAddPacketsPartsResult(storageToken, results, results.All(x => x.Success));
        }

        public static AddAddPacketsPartsResult EmptyResult()
        {
            return new AddAddPacketsPartsResult(null, new List<AddAddPacketPartResult>(), true);
        }
    }

    //public interface IPacketWriter: IDisposable
    //{

    //    void Write(byte[] buffer, int offset, int count);
    //    void SetLength(long length);
    //    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

    //}
    
}
