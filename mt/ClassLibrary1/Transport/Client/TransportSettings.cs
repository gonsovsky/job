using System;

namespace ClassLibrary1.Transport
{
    /// <summary>
    /// настройки сервиса для пакетной передачи данных на сервер
    /// </summary>
    public class TransportSettings
    {
        /// <summary>
        /// лимиты на размеры отправляемых пакетов
        /// </summary>
        public SendMessageSizeLimits PacketSizeLimits { get; set; }

        /// <summary>
        /// Таймаут при Exception-ах возникающих в отправке
        /// </summary>
        public TimeSpan ErrorRetryTimeout { get; set; }

        /// <summary>
        /// Таймаут при Exception-ах возникающих на сервере
        /// </summary>
        public TimeSpan ServerErrorRetryTimeout { get; set; }

        /// <summary>
        /// Таймаут накопления данных
        /// </summary>
        public TimeSpan CollectMinPacketSizeTimeout { get; set; }

        /// <summary>
        /// Таймаут обновления конфигурации
        /// </summary>
        public TimeSpan ConfigurationUpdateTimeout { get; set; }

        ///// <summary>
        ///// директория для хранения данные об отправке
        ///// </summary>
        //public string SendConfigDirectory { get; set; }

        public TransportSettings()
        {
            this.PacketSizeLimits = new SendMessageSizeLimits
            {
                Min = TransportConstants.DefaultMinClientPacketSize,
                Max = TransportConstants.DefaultMaxClientPacketSize,
            };

            this.ErrorRetryTimeout = TransportConstants.DefaultCommonErrorRetryTimeout;
            this.ServerErrorRetryTimeout = TransportConstants.DefaultCommonServerErrorsTimeout;

            this.CollectMinPacketSizeTimeout = TransportConstants.DefaultCollectMinPacketSizeTimeout;
            this.ConfigurationUpdateTimeout = TransportConstants.DefaultConfigurationUpdateTimeout;
        }

        public SendSettings GetSendSettings()
        {
            return new SendSettings
            {
                ErrorRetryTimeout = ErrorRetryTimeout,
                SizeLimits = PacketSizeLimits,
            };
        }
    }
}
