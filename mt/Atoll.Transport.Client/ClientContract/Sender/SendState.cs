using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Contract
{
    /// <summary>
    /// Часть данных состояния в процессе работы отправителя
    /// </summary>
    public class SendState
    {

        public DateTime? LastConfigurationsUpdate { get; set; }

        public DateTime? FirstPacketsSizeEvaluation { get; set; }

        public SendState Clone()
        {
            return new SendState
            {
                LastConfigurationsUpdate = this.LastConfigurationsUpdate,
                FirstPacketsSizeEvaluation = this.FirstPacketsSizeEvaluation,
            };
        }
    }
}
