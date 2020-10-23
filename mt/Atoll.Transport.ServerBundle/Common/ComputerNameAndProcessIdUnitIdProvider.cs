using System;
using System.Diagnostics;

namespace Atoll.Transport.ServerBundle
{
    public class ComputerNameAndProcessIdUnitIdProvider : IUnitIdProvider, IDisposable
    {
        //private Timer timer;
        private string id;

        public ComputerNameAndProcessIdUnitIdProvider(TimeSpan updateTimeout)
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
            this.id = Environment.MachineName + Process.GetCurrentProcess().Id;
        }

        public string GetId()
        {
            return this.id;
        }
    }
}
