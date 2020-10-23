using System;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public interface ITimeService : IDisposable
    {
        DateTime GetCurrentUtcTime(CancellationToken cancellationToken = default(CancellationToken));
    }

}
