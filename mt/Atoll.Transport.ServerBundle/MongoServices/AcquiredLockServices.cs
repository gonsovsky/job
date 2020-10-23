using System;

namespace Atoll.Transport.ServerBundle
{
    public class AcquiredLockServices : IDisposable
    {
        public AcquiredDbParameters AcquiredDbParameters { get; }
        public ILockService LockService { get; }
        public ITimeService TimeService { get; }
        public ILeaseLockObject LockObject { get; }

        public AcquiredLockServices(AcquiredDbParameters acquiredDbParameters, ILockService lockService, ITimeService timeService, ILeaseLockObject lockObject)
        {
            this.AcquiredDbParameters = acquiredDbParameters;
            this.LockService = lockService;
            this.TimeService = timeService;
            this.LockObject = lockObject;
        }

        public void Dispose()
        {
            (this.LockObject as IDisposable)?.Dispose();
            (this.LockService as IDisposable)?.Dispose();
            (this.TimeService as IDisposable)?.Dispose();
        }
    }

}
