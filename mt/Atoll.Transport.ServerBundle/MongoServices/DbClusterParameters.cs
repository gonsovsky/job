using System;
using System.Collections.Generic;

namespace Atoll.Transport.ServerBundle
{
    public class DbClusterServiceParameters
    {
        public IList<string> ConnStringsOrUrls { get; set; }
        public string DatabaseName { get; set; }
        public TimeSpan LeaseCheckTimeout { get; set; }
        public TimeSpan LeaseLostTimeout { get; set; }
        public TimeSpan UpdateServerTimeInterval { get; set; }

        public DbClusterServiceParameters(string[] connStringsOrUrls,
            string databaseName, 
            TimeSpan? leaseCheckTimeout = null, 
            TimeSpan? leaseLostTimeout = null, 
            TimeSpan? updateServerTimeInterval = null)
        {
            this.ConnStringsOrUrls = connStringsOrUrls;
            this.DatabaseName = databaseName;

            this.LeaseCheckTimeout = leaseCheckTimeout ?? TransportConstants.DefaultLeaseCheckTimeout;
            this.LeaseLostTimeout = leaseLostTimeout ?? TransportConstants.DefaultLeaseLostTimeout;
            this.UpdateServerTimeInterval = updateServerTimeInterval ?? TransportConstants.DefaultUpdateServerTimeInterval;
        }
    }

}
