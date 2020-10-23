using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Linq;

namespace WebApplication18.Transport
{
    public interface IPacketsStore
    {

        Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentifierData agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken));

        Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentifierData agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken));

        Task<bool> CheckIfHasReservationsAsync(CancellationToken requestAborted);

        Task<bool> PingAsync(CancellationToken requestAborted);
    }
}
