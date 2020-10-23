using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class SendStateStore : ISendStateStore
    {
        private SendState current = new SendState();

        public void CheckedPacketsSize()
        {
            if (current.FirstPacketsSizeEvaluation == null)
            {
                var newValue = current.Clone();
                newValue.FirstPacketsSizeEvaluation = DateTime.UtcNow;
                this.Save(newValue);
            }
        }

        public void ConfigurationsUpdated()
        {
            var newValue = current.Clone();
            newValue.LastConfigurationsUpdate = DateTime.UtcNow;
            this.Save(newValue);
        }

        public SendState Get()
        {
            return this.current;
        }

        private void Save(SendState sendState)
        {
            this.current = sendState;
        }
    }
}
