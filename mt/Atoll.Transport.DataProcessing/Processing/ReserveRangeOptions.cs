using System;

namespace Atoll.Transport.DataProcessing
{
    public class ReserveRangeOptions
    {
        public TimeSpan LeaseTimeout { get; set; }
        public ReservationRangeLimits NeedReserveRange { get; set; }
        public bool HasDataInPreviousProcessing { get; set; }
        public int FullScanAfterNQueries { get; set; }
    }
}
