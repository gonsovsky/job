using System;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public static class TimerUtils
    {
        public static Timer CreateNonOverlapped(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback((object state1) =>
            {
                callback(state1);
                try
                {
                    timer.Change(period, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            }), state, dueTime, period);

            return timer;
        }
    }
}
