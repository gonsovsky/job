using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atoll.Transport.ServerBundle
{
    /// <remarks>
    /// Сервис "получения" И управления состоянием lease-локов для набора("кластера") баз данных
    /// </remarks>
    public interface IDbClusterService : IDisposable
    {
        Task StartSearchTask(CancellationToken token);
        Task DisposeAsync(CancellationToken token);

        bool CheckIfHasLease();
        ReserveResult RenewLeaseReservations(/*CancellationToken token*/);
        ReserveResult CheckLeaseReservations();        
    }

}
