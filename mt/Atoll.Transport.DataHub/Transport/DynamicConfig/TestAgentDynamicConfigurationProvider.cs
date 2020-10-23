using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Atoll.Transport.DataHub
{

    public class TestAgentDynamicConfigurationProvider : IAgentDynamicConfigurationProvider
    {
        public string ProviderKey
        {
            get
            {
                return "test";
            }
        }

        private string config = "{test:1}";

        public AgentDynamicConfiguration GetConfigurationData(AgentIdentity idData)
        {
            var cnf = this.config;
            return new AgentDynamicConfiguration
            {
                Token = config,
                StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(cnf)),
            };
        }
    }
}
