using Atoll.Transport.ServerBundle;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.DataHub
{
    public class DbClusterPacketsStore : IPacketsStore, IDisposable
    {

        public class CachePair
        {
            public AcquiredDbParameters DbParameters { get; set; }
            public IPacketsStore Store { get; set; }
        }

        private IDbClusterService dbClusterService;
        private readonly Func<AcquiredDbParameters, IPacketsStore> dbInstanceStoreFactory;
        private CachePair cachedPacketsStorePair;
        private object cachePairsLock = new object();

        //private int counter;

        public DbClusterPacketsStore(IDbClusterService dbClusterService, Func<AcquiredDbParameters, IPacketsStore> dbInstanceStoreFactory)
        {
            this.dbClusterService = dbClusterService;
            this.dbInstanceStoreFactory = dbInstanceStoreFactory;
        }

        //private CachePair[] GetStoresCachePairs(CachePair[] oldCachedStores, AcquiredLockServices[] services)
        //{
        //    var newCachedStores = services.Select(x => {
        //        var dbParameters = x.AcquiredDbParameters;
        //        var old = oldCachedStores?.FirstOrDefault(o => o.DbParameters.Equals(dbParameters));
        //        return old ?? new CachePair
        //        {
        //            DbParameters = dbParameters,
        //            Store = dbInstanceStoreFactory(dbParameters),
        //        };
        //    }).ToArray();

        //    return newCachedStores;
        //}

        //private IPacketsStore FromCachePairs(CachePair[] cachePairs, AcquiredDbParameters acquiredDbParameters)
        //{
        //    return cachePairs?.FirstOrDefault(x => x.DbParameters.Equals(acquiredDbParameters))?.Store;
        //}

        // есть негативный низковерятный вариант когда каждый раз будет резервироваться новая бд, тогда закешированной значение будет постянно меняться (а в нашем случае у него есть логика инициализации на которую тратится время)
        private IPacketsStore GetPacketsStore(AcquiredLockServices services)
        {
            var acquiredDbParameters = services.AcquiredDbParameters;
            var storePair = this.cachedPacketsStorePair;
            if (storePair == null || !acquiredDbParameters.Equals(storePair.DbParameters))
            {
                lock (this.cachePairsLock)
                {
                    if (storePair == null || !acquiredDbParameters.Equals(storePair.DbParameters))
                    {
                        storePair = new CachePair
                        {
                            DbParameters = acquiredDbParameters,
                            Store = this.dbInstanceStoreFactory(acquiredDbParameters),
                        };

                        this.cachedPacketsStorePair = storePair;
                    }
                    else
                    {
                        storePair = this.cachedPacketsStorePair;
                    }
                }
            }

            return storePair.Store;
        }

        private void ThrowIfNotValid(ReserveResult leaseResult)
        {
            if (!leaseResult.IsSuccess)
            {
                throw new InvalidOperationException("Could not reserve db for events storing");
            }
        }

        public async Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentity agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leaseResult = this.dbClusterService.RenewLeaseReservations();
            this.ThrowIfNotValid(leaseResult);
            // выбираем бд RoundRobin-ом
            var store = this.GetPacketsStore(leaseResult.ReservedServices);
            return await store.AddIfNotExistsPacketPartAsync(agentId, request, cancellationToken);
        }

        public async Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentity agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken))
        {
            var leaseResult = this.dbClusterService.RenewLeaseReservations();
            this.ThrowIfNotValid(leaseResult);
            // выбираем бд RoundRobin-ом
            var store = this.GetPacketsStore(leaseResult.ReservedServices);
            return await store.AddIfNotExistsPacketsPartsAsync(agentId, requests, cancellationToken);
        }

        //private async Task<ReserveResult> RenewAndValidateLease(CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        //    {
        //        cts.CancelAfter()
        //        try
        //        {
        //            var leaseResult = await this.dbClusterService.RenewLeaseAsync(cts.Token);
        //            this.ThrowIfNotValid(leaseResult);
        //            return leaseResult;
        //        }
        //        catch (OperationCanceledException) when(!cancellationToken.IsCancellationRequested)
        //        {

        //            throw;
        //        }
        //    }
        //}

        public void Dispose()
        {
            this.dbClusterService?.Dispose();
        }

        public bool CheckIfHasReservations()
        {
            return this.dbClusterService.CheckIfHasLease();
        }

        public async Task<bool> PingAsync(CancellationToken token)
        {
            try
            {
                var leaseResult = this.dbClusterService.CheckLeaseReservations();
                if (leaseResult.IsSuccess && leaseResult.ReservedServices != null)
                {
                    // TODO требуется возвращать более подробную информацию! Чтобы было видно что бд перестала отвечать...
                    var store = this.GetPacketsStore(leaseResult.ReservedServices);
                    var isAlive = await store.PingAsync(token);
                    if (isAlive)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }

            return false;
        }
    }

}
