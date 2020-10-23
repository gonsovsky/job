using Atoll.UtilsBundle.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.ServerBundle
{

    /// <remarks>
    /// сервис для "получения" lease-локов для набора("кластера") баз данных (пока реализован ограничено)
    /// </remarks>
    public class DbClusterLeaseService : IDbClusterService
    {
        private IDbClusterSearchUtils dbClusterService;
        private readonly DbClusterServiceParameters dbClusterParameters;
        private AcquiredLockServices dbServices;
        private readonly DbClusterReservationsSettings settings;
        private readonly object sync = new object();
        private readonly object disposeLock = new object();
        private bool isDisposed = false;

        private CancellationTokenSource onDisposeCancelSource;

        public DbClusterLeaseService(IDbClusterSearchUtils dbClusterService, DbClusterServiceParameters dbClusterParameters, DbClusterReservationsSettings settings)
        {
            this.onDisposeCancelSource = new CancellationTokenSource();
            this.dbClusterService = dbClusterService;
            this.dbClusterParameters = dbClusterParameters;
            this.settings = settings;
        }

        private Task executingTask;

        public Task StartSearchTask(CancellationToken token)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("dbServices");
            }

            this.executingTask = this.SearchTaskBody(token);

            // If the task is completed then return it,
            // this will bubble cancellation and failure to the caller
            if (this.executingTask.IsCompleted)
            {
                return this.executingTask;
            }

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public async Task DisposeAsync(CancellationToken token)
        {
            // Stop called without start
            if (this.executingTask == null)
            {
                return;
            }

            try
            {
                // Signal cancellation to the executing method
                this.Dispose();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(this.executingTask, Task.Delay(Timeout.Infinite, token));
            }
        }

        private Task SearchTaskBody(CancellationToken token)
        {
            return Task.Factory.StartNew(() =>
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token, this.onDisposeCancelSource.Token))
                {
                    //
                    var cancellationToken = cts.Token;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            CancellationToken leaseLostToken;
                            lock (this.sync)
                            {
                                // резервируем бд
                                if (this.dbServices?.LockObject.CheckAndUpdateLease(cancellationToken) == true)
                                {
                                    // lease найдена и действует
                                }
                                else
                                {
                                    // lease НЕ найдена или НЕ действует
                                    this.dbServices?.Dispose();
                                    this.dbServices = null;
                                    // при this.dbServices != null - lease найдена и действует
                                    this.dbServices = this.dbClusterService.AcquireLockAndServicesForAnyNodeOrDefault(dbClusterParameters, cancellationToken);
                                }

                                if (this.dbServices != null)
                                {
                                    // lease найдена и действует
                                    leaseLostToken = this.dbServices.LockObject.GetLoseLeaseToken();
                                }
                            }

                            // lease найдена и действует
                            if (leaseLostToken != null)
                            {
                                // ждём потери или отмены
                                WaitHandle.WaitAny(new WaitHandle[] { leaseLostToken.WaitHandle, cancellationToken.WaitHandle });
                                // при потере может стоит производить дополнительные действия?
                                continue;
                            }

                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            // lease не зарезервировалась
                            Task.Delay(this.settings.SearchDbTimeout, cancellationToken).Wait(cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            // завершаем
                            return;
                        }
                        catch (Exception)
                        {
                            // ignore ??
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning);
        }

        public ReserveResult RenewLeaseReservations(/*CancellationToken token*/)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("dbServices");
            }

            lock (this.sync)
            {
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException("dbServices");
                }

                try
                {
                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(/*token,*/ this.onDisposeCancelSource.Token))
                    {
                        // резервируем бд
                        if (this.dbServices?.LockObject.CheckAndUpdateLease(cts.Token) == true)
                        {
                            return ReserveResult.FromValueResult(true, this.dbServices);
                        }
                        else
                        {
                            // lease НЕ найдена, потеряна или НЕ действует
                            return ReserveResult.FailResult();
                        }
                    }        
                }
                //catch (OperationCanceledException)
                //{
                //    return ReserveResult.FailResult();
                //}
                catch (Exception)
                {
                    // ignore ??
                    return ReserveResult.FailResult();
                }
            }
        }

        public ReserveResult CheckLeaseReservations()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("dbServices");
            }

            var dbServices = this.dbServices;
            try
            {
                // резервируем бд
                if (dbServices?.LockObject.CheckLease() == true)
                {
                    return ReserveResult.FromValueResult(true, dbServices);
                }
                else
                {
                    // lease НЕ найдена, потеряна или НЕ действует
                    return ReserveResult.FailResult();
                }
            }
            catch (Exception)
            {
                // ignore ??
                return ReserveResult.FailResult();
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
                if (this.isDisposed)
                {
                    return;
                }

                this.onDisposeCancelSource.SafeCancel();
                lock (this.sync)
                {
                    if (this.isDisposed)
                    {
                        return;
                    }

                    this.dbServices.SafeDispose();
                    this.dbServices = null;
                    this.onDisposeCancelSource.SafeDispose();
                    this.isDisposed = true;
                }
            }      
        }

        public bool CheckIfHasLease()
        {
            return this.dbServices?.LockObject.CheckLease() ?? false;
        }
    }

}
