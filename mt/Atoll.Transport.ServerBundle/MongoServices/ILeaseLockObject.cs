using System;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public interface ILeaseLockObject : IDisposable
    {
        bool CheckLease();
        bool CheckAndUpdateLease(CancellationToken token);
        bool CheckAndUpdateOrAcquireLease(CancellationToken token);
        CancellationToken GetLoseLeaseToken();
    }

}
