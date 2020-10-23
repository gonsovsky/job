using System;

namespace Atoll.Transport.DataProcessing
{
    /// <summary>
    /// Контейнер обработчика событий.
    /// </summary>
    public sealed class ProcessorContainer
    {

        public ProcessorContainer(IPacketsProcessor processor, AdaptiveInterval adaptiveInterval = null)
        {
            this.Processor = processor;

            this.QuarantineEndTime = DateTime.MinValue;

            this.quarantineTimeSeconds = 10;
            this.adaptiveInterval = adaptiveInterval ?? new AdaptiveInterval(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5000), TimeSpan.FromMilliseconds(100));
        }

        private readonly int quarantineTimeSeconds;

        private readonly AdaptiveInterval adaptiveInterval;

        public bool CanProcess { get { return this.QuarantineEndTime < DateTime.UtcNow; } }

        public IPacketsProcessor Processor { get; }

        public DateTime QuarantineEndTime { get; private set; }

        /// <summary>
        /// Признак, что сообщение NoData уже логировалось.
        /// </summary>
        public bool NoDataReported { get; set; }

        public void Quarantine()
        {
            this.QuarantineEndTime = DateTime.UtcNow.AddSeconds(this.quarantineTimeSeconds);
            this.adaptiveInterval.Reset();
        }

        public void Suspend()
        {
            this.QuarantineEndTime = DateTime.UtcNow.Add(this.adaptiveInterval.GetNextInterval());
        }

        /// <summary>
        /// Сброс адаптивной паузы между запусками процессора.
        /// </summary>
        public void ResetSuspendIntervals()
        {
            this.adaptiveInterval.Reset();
        }

        #region Nested types

        public sealed class AdaptiveInterval
        {

            public AdaptiveInterval(TimeSpan minMs, TimeSpan maxMs, TimeSpan stepMs)
            {
                this.minMs = minMs;
                this.maxMs = maxMs;
                this.stepMs = stepMs;

                this.currentMs = this.minMs;
            }

            private readonly TimeSpan minMs;

            private readonly TimeSpan maxMs;

            private readonly TimeSpan stepMs;

            private TimeSpan currentMs;

            public TimeSpan GetNextInterval()
            {
                var intervalToReturn = this.currentMs;
                if (this.currentMs < this.maxMs)
                {
                    this.currentMs += this.stepMs;
                    if (this.currentMs > this.maxMs)
                        this.currentMs = this.maxMs;
                }

                return intervalToReturn;
            }

            public void Reset()
            {
                this.currentMs = this.minMs;
            }

        }

        #endregion

    }
}
