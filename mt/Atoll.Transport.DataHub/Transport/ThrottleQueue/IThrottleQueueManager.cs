using System;

namespace Atoll.Transport.DataHub
{
    public class WaitOnFailOptions
    {
        public TimeSpan WaitOnFailTimeout { get; private set; }

        public WaitOnFailOptions(TimeSpan waitOnFailTimeout)
        {
            this.WaitOnFailTimeout = waitOnFailTimeout;
        }
    }

    public interface IThrottleQueueManager
    {
        bool TryAccept(string id, ThrottleParams throttleParams, out WaitOnFailOptions waitOnFailOptions);
        void Release(string id);
    }
}
