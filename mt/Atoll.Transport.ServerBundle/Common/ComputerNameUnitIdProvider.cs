using System;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{

    public class ComputerNameUnitIdProvider : IUnitIdProvider, IDisposable
    {
        //private Timer timer;
        private string machineName;

        public ComputerNameUnitIdProvider(TimeSpan updateTimeout)
        {
            //this.timer = TimerUtils.CreateNonOverlapped(this.Update, null, TimeSpan.Zero, updateTimeout);
            this.Update();
        }

        public void Dispose()
        {
            //this.timer?.Dispose();
            //this.timer = null;
        }

        private void Update(object state)
        {
            this.Update();
        }

        private void Update()
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
