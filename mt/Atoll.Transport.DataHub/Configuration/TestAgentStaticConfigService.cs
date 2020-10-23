namespace Atoll.Transport.DataHub
{
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
