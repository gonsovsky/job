using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class SendSettings
    {
        public TimeSpan ErrorRetryTimeout { get; set; }
        public SendMessageSizeLimits SizeLimits { get; set; }
    }

    public static class TransportSettingsExtensions
    {
        public static SendSettings GetSendSettings(this TransportSettings settings)
        {
            return new SendSettings
            {
                ErrorRetryTimeout = settings.ErrorRetryTimeout,
                SizeLimits = settings.PacketSizeLimits,
            };
        }
    }
}
