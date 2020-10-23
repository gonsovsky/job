using System;

namespace Atoll.Transport.ServerBundle
{
    public class DbClusterReservationsSettings
    {
        public TimeSpan SearchDbTimeout { get; private set; }

        public DbClusterReservationsSettings(TimeSpan? searchDbTimeout = null)
        {
            this.SearchDbTimeout = searchDbTimeout ?? TransportConstants.DefaultSearchDbForReservationTimeout;
        }
    }

}
