using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public interface ILockService
    {
        ILeaseLockObject CreateLockOrNull(CancellationToken token = default(CancellationToken));

        bool TryAcquireOrUpdateLease(CancellationToken token = default(CancellationToken));
    }

}
