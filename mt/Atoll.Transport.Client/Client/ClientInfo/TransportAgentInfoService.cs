using System;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class TransportAgentInfoService : ITransportAgentInfoService
    {
        class TransportTokens
        {
            public string DbToken { get; set; }
            public string ConfigToken { get; set; }
            public int ConfigVersion { get; set; }
        }

        private TransportTokens current = new TransportTokens();
        private readonly IComputerIdentityProvider computerIdentityProvider;

        public TransportAgentInfoService(IComputerIdentityProvider computerIdentityProvider)
        {
            this.computerIdentityProvider = computerIdentityProvider;
        }

        public TransportAgentInfo Get()
        {
            var identity = computerIdentityProvider.GetIdentity();
            var domain = identity.DomainName;
            var computerName = identity.ComputerName;

            return new TransportAgentInfo
            {
                Domain = domain,
                ComputerName = computerName,
                //OrganizationUnit = 

                DbToken = current.DbToken,
                ConfigVersion = current.ConfigVersion,
                ConfigToken = current.ConfigToken,
            };
        }

        public void SetDbToken(TransportDbTokenData data)
        {
            current = new TransportTokens
            {
                DbToken = data.DbToken,
                ConfigToken = this.current.ConfigToken,
                ConfigVersion = this.current.ConfigVersion,
            };
        }

        public void SetStaticConfig(TransportStaticConfig data)
        {
            current = new TransportTokens
            {
                DbToken = this.current.DbToken,
                ConfigToken = data.ConfigToken,
                ConfigVersion = data.ConfigVersion,
            };
        }
    }
}
