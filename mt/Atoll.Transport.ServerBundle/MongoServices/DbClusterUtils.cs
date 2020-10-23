using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    public class DbClusterSearchUtils : IDbClusterSearchUtils
    {
        private readonly IDbServicesFactory dbServicesFactory;

        public DbClusterSearchUtils(IDbServicesFactory dbServicesFactory)
        {
            this.dbServicesFactory = dbServicesFactory;
        }

        public AcquiredLockServices AcquireLockAndServicesForAnyUrlsOrDefault(IList<string> urls,
            string databaseName,
            TimeSpan leaseCheckTimeout,
            TimeSpan leaseLostTimeout,
            TimeSpan updateServerTimeInterval,
            CancellationToken token)
        {
            foreach (var url in urls)
            {
                if (token.IsCancellationRequested)
                {
                    return null;
                }

                IDbService dbService = null;
                try
                {
                    dbService = this.dbServicesFactory.GetDbService(url, databaseName, leaseCheckTimeout, leaseLostTimeout, updateServerTimeInterval);
                    //mongoDbService = new MongoDbService(url, databaseName, leaseCheckTimeout, leaseLostTimeout, updateServerTimeInterval);

                    ILeaseLockObject leaseLock = null;
                    try
                    {
                        leaseLock = dbService.CreateLockOrNull(token);
                        if (leaseLock != null)
                        {
                            return new AcquiredLockServices(new AcquiredDbParameters
                            {
                                ConnStringOrUrl = url,
                                DatabaseName = databaseName,
                            }, dbService, dbService, leaseLock);
                        }
                        else
                        {
                            leaseLock?.Dispose();
                            dbService.Dispose();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        leaseLock?.Dispose();
                        dbService?.Dispose();
                        // отмена
                        return null;
                    }
                    catch
                    {
                        leaseLock?.Dispose();
                        dbService?.Dispose();
                        return null;
                    }
                }
                catch (OperationCanceledException)
                {
                    dbService?.Dispose();
                    // отмена
                    return null;
                }
                catch
                {
                    dbService?.Dispose();
                    continue;
                }
            }

            return null;
        }

        public AcquiredLockServices AcquireLockAndServicesForAnyNodeOrDefault(DbClusterServiceParameters parameters, CancellationToken token)
        {
            return this.AcquireLockAndServicesForAnyUrlsOrDefault(parameters.ConnStringsOrUrls, parameters.DatabaseName, parameters.LeaseCheckTimeout, parameters.LeaseLostTimeout, parameters.UpdateServerTimeInterval, token);
        }
    }

}
