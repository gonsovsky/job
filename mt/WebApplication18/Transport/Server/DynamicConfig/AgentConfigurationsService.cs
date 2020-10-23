using Atoll.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication18.Transport
{
    /// <inheritdoc />
    public class AgentConfigurationsService : IAgentConfigurationsService
    {
        private IDictionary<string, IAgentDynamicConfigurationProvider> configProviders = new ConcurrentDictionary<string, IAgentDynamicConfigurationProvider>();

        public AgentConfigurationsService()
        {

        }

        /// <inheritdoc />
        public void AddWithKeyOrThrow(IAgentDynamicConfigurationProvider provider)
        {
            this.configProviders.Add(provider.ProviderKey, provider);
        }

        /// <inheritdoc />
        public IList<ConfigurationResponse> GetConfigurationResponses(MessageHeaders messageHeaders, IReadOnlyCollection<ConfigurationRequestDataItem> configurationsOnAgent)
        {
            var idData = messageHeaders.GetAgentIdData();

            var list = new List<ConfigurationResponse>();

            try
            {
                foreach (var provider in configProviders.Values)
                {
                    var stats = configurationsOnAgent.FirstOrDefault(x => x.ProviderKey == provider.ProviderKey);
                    var configData = provider.GetConfigurationData(idData);
                    if (stats != null)
                    {
                        if (stats.IsCompleted && stats.Token == configData.Token && stats.Token != null)
                        {
                            continue;
                        }
                    }

                    //if (stats.StartPosition > 0)
                    //{

                    //}

                    list.Add(new ConfigurationResponse
                    {
                        ProviderKey = provider.ProviderKey,
                        StartPosition = 0,
                        EndPosition = null,
                        IsFinal = true,
                        Stream = configData.StreamFactory(),
                        Token = configData.Token,
                    });
                }
            }
            catch (Exception)
            {
                foreach (var item in list)
                {
                    item.Stream.Dispose();
                }
                throw;
            }

            return list;
        }
    }
}
