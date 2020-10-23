using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication18.Transport
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

        public AgentDynamicConfiguration GetConfigurationData(AgentIdentifierData idData)
        {
            var cnf = this.config;
            return new AgentDynamicConfiguration
            {
                Token = cnf.GetHashCode().ToString(),
                StreamFactory = () => new MemoryStream(Encoding.UTF8.GetBytes(cnf)),
            };
        }
    }
}
