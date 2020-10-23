using Atoll.UtilsBundle.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Atoll.Transport.ServerBundle
{
    // TODO ЭТОТ КЛАСС НАДО ПЕРЕПИСАТЬ (не забываем Interlocked для нужных полей) !!!
    public class LeaseLockObject : ILeaseLockObject
    {
        private DateTime leaseUpdateTime;
        private bool hasCheckedLease;
        //private bool hasLostLease;
        private readonly ILockService lockService;
        private readonly TimeSpan leaseCheckTimeout;
        private readonly TimeSpan leaseLostTimeout;
        private Timer timer;
        private readonly object sync = new object();
        private readonly object disposeLock = new object();
        private bool isDisposed;
        //private object
        private readonly CancellationTokenSource disposeCts;

        public LeaseLockObject(ILockService lockService, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout)
        {
            this.lockService = lockService;
            this.leaseCheckTimeout = leaseCheckTimeout;
            this.leaseLostTimeout = leaseLostTimeout;
            this.disposeCts = new CancellationTokenSource();
            this.timer = new Timer(this.UpdateLeaseTimeCallback, null, leaseCheckTimeout, leaseCheckTimeout);
        }

        private void UpdateLeaseTimeCallback(object state)
        {
            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    return;
                }

                var newPeriod = this.leaseCheckTimeout;
                var dateTimeNow = DateTime.UtcNow;
                if (dateTimeNow < this.leaseUpdateTime + this.leaseCheckTimeout)
                {
                    // пока не требуется обновлять, но момент в который потребуется обновить поменялся (т.к. в CheckAndUpdateLease есть обновление)
                    newPeriod = this.leaseUpdateTime + this.leaseCheckTimeout - dateTimeNow;
                }
                else
                {
                    this.UpdateLease();
                    newPeriod = this.leaseUpdateTime + this.leaseCheckTimeout - DateTime.UtcNow;
                    if (newPeriod < TimeSpan.Zero)
                    {
                        newPeriod = TimeSpan.FromMilliseconds(50);
                    }
                }

                try
                {
                    this.timer?.Change(newPeriod, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
            }
        }

        private void UpdateLease(CancellationToken cToken = default(CancellationToken))
        {          
            bool hasLease;
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cToken, this.disposeCts.Token))
            {
                try
                {
                    hasLease = this.lockService.TryAcquireOrUpdateLease(cts.Token);
                }
                //catch (OperationCanceledException)
                //{
                //    hasLease = false;
                //}
                catch (Exception)
                {
                    // при ошибке считаем lease "временно" потерянной, так мы отсечём лишних клиентов и снимем нагрузку, а lease впоследствии востановим по бд
                    hasLease = false;
                }
            }            

            this.hasCheckedLease = hasLease;
            this.leaseUpdateTime = DateTime.UtcNow;

            // лиза потеряна
            if (!this.hasCheckedLease)
            {
                List<CancellationTokenSource> loseLeaseCtsListLocal = null;
                lock (this.sync)
                {
                    if (!this.isDisposed)
                    {
                        loseLeaseCtsListLocal = this.loseLeaseCtsList;
                        this.loseLeaseCtsList = new List<CancellationTokenSource>();
                    }
                }

                if (loseLeaseCtsListLocal != null)
                {
                    foreach (var loseLeaseCts in loseLeaseCtsListLocal)
                    {
                        loseLeaseCts.SafeCancel();
                    }
                }
            }
        }

        public bool CheckAndUpdateLease(CancellationToken token)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("lease lock");
            }

            // данные до истечения таймаута проверки lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseCheckTimeout)
            {
                return this.hasCheckedLease;
            }

            // данные до истечения таймаута жизни lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseLostTimeout)
            {
                if (!this.hasCheckedLease)
                {
                    this.UpdateLease(token);
                    return false;
                }
                else
                {
                    return this.hasCheckedLease;
                }
            }
            else
            {
                // после истечения
                return false;
            }
        }

        public bool CheckAndUpdateOrAcquireLease(CancellationToken token)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("lease lock");
            }

            // данные до истечения таймаута проверки lease
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseCheckTimeout)
            {
                return this.hasCheckedLease;
            }

            // данные до истечения таймаута жизни lease, но уже после leaseCheckTimeout
            if (DateTime.UtcNow < this.leaseUpdateTime + this.leaseLostTimeout)
            {
                if (!this.hasCheckedLease)
                {
                    return false;
                }
                else
                {
                    this.timer?.Change(50, Timeout.Infinite);
                    return this.hasCheckedLease;
                }
            }
            else
            {
                // после истечения
                this.UpdateLease(token);
                return this.hasCheckedLease;
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            lock (this.disposeLock)
            {
                this.disposeCts.SafeCancel();
                lock (this.sync)
                {
                    this.timer?.Dispose();
                    this.timer = null;

                    this.disposeCts.SafeDispose();

                    var loseLeaseCtsListLocal = this.loseLeaseCtsList;
                    this.loseLeaseCtsList = null;
                    if (loseLeaseCtsListLocal != null)
                    {
                        foreach (var loseLeaseTcs in loseLeaseCtsListLocal)
                        {
                            loseLeaseTcs.Dispose();
                        }
                    }

                    this.isDisposed = true;
                }
            }
        }

        private List<CancellationTokenSource> loseLeaseCtsList = new List<CancellationTokenSource>();
        public CancellationToken GetLoseLeaseToken()
        {
            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("lease lock");
                }

                var cts = new CancellationTokenSource();
                this.loseLeaseCtsList.Add(cts);
                if (!this.hasCheckedLease)
                {
                    cts.SafeCancel();
                }
                return cts.Token;
            }
        }

        public bool CheckLease()
        {
            return this.hasCheckedLease;
        }
    }

}
