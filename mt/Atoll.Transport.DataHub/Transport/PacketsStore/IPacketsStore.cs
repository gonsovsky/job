using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Threading;
using System.Linq;

namespace Atoll.Transport.DataHub
{
    public interface IPacketsStore
    {

        Task<AddAddPacketPartResult> AddIfNotExistsPacketPartAsync(AgentIdentity agentId, AddPacketPartRequest request, CancellationToken cancellationToken = default(CancellationToken));

        Task<AddAddPacketsPartsResult> AddIfNotExistsPacketsPartsAsync(AgentIdentity agentId, IList<AddPacketPartRequest> requests, CancellationToken cancellationToken = default(CancellationToken));

        bool CheckIfHasReservations();

        Task<bool> PingAsync(CancellationToken cancellationToken);

        //DateTime? GetServerDateTime();
    }
}
