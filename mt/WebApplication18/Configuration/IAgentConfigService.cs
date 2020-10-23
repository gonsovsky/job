using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication18.Configuration
{

    public interface IAgentStaticConfigService
    {
        bool ReloadConfig();
        ConfigData GetConfigData();
    }

    public class AgentStaticConfigService : IAgentStaticConfigService
    {
        public ConfigData GetConfigData()
        {
            return new ConfigData
            {
                Config = "temp",
                ConfigToken = "temp",
                ConfigVersion = 1
            };
        }

        public bool ReloadConfig()
        {
            return false;
        }
    }
}
