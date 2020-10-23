using System;
using System.Threading;

namespace Atoll.Transport.DataHub
{
    public class LimitConcurrentQueueManager : IThrottleQueueManager
    {
        private readonly int _max;
        private int _count = 0;
        private readonly object syncObj = new object();

        public WaitOnFailOptions WaitOnFailOptions { get; }

        public LimitConcurrentQueueManager(int concurrentRequests, TimeSpan waitOnFailTimeout)
        {
            this._max = concurrentRequests;
            this.WaitOnFailOptions = new WaitOnFailOptions(waitOnFailTimeout);
        }

        public void Release(string id)
        {
            Interlocked.Decrement(ref this._count);
        }

        public bool TryAccept(string id, ThrottleParams throttleParams, out WaitOnFailOptions waitOnFailOptions)
        {
            var count = _count;

            // Exit if count == MaxValue as incrementing would overflow.
            while (count < _max && count != int.MaxValue)
            {
                var prev = Interlocked.CompareExchange(ref _count, count + 1, count);
                if (prev == count)
                {
                    waitOnFailOptions = default(WaitOnFailOptions);
                    return true;
                }

                // Another thread changed the count before us. Try again with the new counter value.
                count = prev;
            }

            waitOnFailOptions = this.WaitOnFailOptions;
            return false;
        }
    }

    //internal class FiniteCounter : ResourceCounter
    //{
    //    private readonly long _max;
    //    private long _count;

    //    public FiniteCounter(long max)
    //    {
    //        if (max < 0)
    //        {
    //            throw new ArgumentOutOfRangeException(CoreStrings.NonNegativeNumberRequired);
    //        }

    //        _max = max;
    //    }

    //    public override bool TryLockOne()
    //    {
    //        var count = _count;

    //        // Exit if count == MaxValue as incrementing would overflow.

    //        while (count < _max && count != long.MaxValue)
    //        {
    //            var prev = Interlocked.CompareExchange(ref _count, count + 1, count);
    //            if (prev == count)
    //            {
    //                return true;
    //            }

    //            // Another thread changed the count before us. Try again with the new counter value.
    //            count = prev;
    //        }

    //        return false;
    //    }

    //    public override void ReleaseOne()
    //    {
    //        Interlocked.Decrement(ref _count);

    //        Debug.Assert(_count >= 0, "Resource count is negative. More resources were released than were locked.");
    //    }

    //    // for testing
    //    internal long Count
    //    {
    //        get => _count;
    //        set => _count = value;
    //    }
    //}
}
