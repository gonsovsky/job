using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    /// <summary>
    /// Сервис для получения лока
    /// </summary>
    public interface IDbClusterSearchUtils
    {
        AcquiredLockServices AcquireLockAndServicesForAnyUrlsOrDefault(IList<string> connStringsOrUrls,
            string databaseName,
            TimeSpan leaseCheckTimeout,
            TimeSpan leaseLostTimeout,
            TimeSpan updateServerTimeInterval,
            CancellationToken token);

        AcquiredLockServices AcquireLockAndServicesForAnyNodeOrDefault(DbClusterServiceParameters parameters, CancellationToken token);
    }

}
