using System;

namespace ClassLibrary1.Transport
{


    public class TransportDbTokenData
    {
        public string DbToken { get; set; }
    }

    public class TransportStaticConfig
    {
        public string ConfigToken { get; set; }
        public int ConfigVersion { get; set; }
    }


    /// <summary>
    /// Сервис получения данных об агенте для отправки сообщений
    /// </summary>
    public interface ITransportAgentInfoService
    {
        TransportAgentInfo Get();
        void SetDbToken(TransportDbTokenData data);
        void SetStaticConfig(TransportStaticConfig data);
    }

    public class TransportAgentInfoService : ITransportAgentInfoService
    {
        class TransportTokens
        {
            public string DbToken { get; set; }
            public string ConfigToken { get; set; }
            public int ConfigVersion { get; set; }
        }

        private TransportTokens current = new TransportTokens();

        public TransportAgentInfoService()
        {

        }

        public TransportAgentInfo Get()
        {
            //var domain = Domain.GetComputerDomain();
            var domain = Environment.UserDomainName;
            var computerName = Environment.MachineName;

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
