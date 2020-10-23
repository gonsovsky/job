namespace WebApplication18.Transport
{
    public class LimitConcurrentQueueManager : IThrottleQueueManager
    {
        private readonly int concurrentRequests;
        private int currentCount = 0;
        private readonly object syncObj = new object();

        public LimitConcurrentQueueManager(int concurrentRequests)
        {
            this.concurrentRequests = concurrentRequests;
        }

        public void Release(string id)
        {
            lock (syncObj)
            {
                this.currentCount--;
            }
        }

        public bool TryAccept(string id, ThrottleParams throttleParams)
        {
            lock (syncObj)
            {
                if (this.concurrentRequests > this.currentCount)
                {
                    this.currentCount++;
                    return true;
                }

                return false;
            }
        }
    }
}
