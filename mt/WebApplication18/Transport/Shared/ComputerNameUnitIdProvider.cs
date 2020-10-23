using System;
using System.Threading;

namespace WebApplication18.Transport
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

    public class ComputerNameUnitIdProvider : IUnitIdProvider, IDisposable
    {
        //private Timer timer;
        private string machineName;

        public ComputerNameUnitIdProvider(TimeSpan updateTimeout)
        {
            //this.timer = TimerUtils.CreateNonOverlapped(this.Update, null, TimeSpan.Zero, updateTimeout);
        }

        public void Dispose()
        {
            //this.timer?.Dispose();
            //this.timer = null;
        }

        private void Update(object state)
        {
            //string hostName = Dns.GetHostName();
            // TODO сделать более надёжную реализацию
            // https://stackoverflow.com/questions/662282/how-do-i-get-the-local-machine-name-in-c
            // ограничение 15 символов ? (у нас в atoll-е везде используется, но могут происходить ошибки)
            this.machineName = Environment.MachineName;
        }

        public string GetId()
        {
            return this.machineName;
        }
    }
}
