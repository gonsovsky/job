using System;
using Atoll.Transport.Client.Contract;
using Coral.Atoll.Utils;

namespace Atoll.Transport.Client.Bundle
{
    /// <summary>
    /// Реализация провайдера получения идентификационной информации о компьютере на основе реальных данных компьютера.
    /// </summary>
    public sealed class RealComputerIdentityProvider : IComputerIdentityProvider
    {

        ComputerIdentity IComputerIdentityProvider.GetIdentity()
        {
            var computerName = Environment.MachineName;
            var domainName = DomainNameInfo.TryGetDomainName(out var dm, out var err) ? dm : DomainNameInfo.NoDomainName;
            return new ComputerIdentity(computerName, domainName);
        }

    }
}
