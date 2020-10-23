using Atoll.Transport;
using System.Collections.Generic;

namespace WebApplication18.Transport
{
    /// <summary>
    /// сервис для формирования конфигураций в ответах запросов транспортной инфраструктуры
    /// </summary>
    public interface IAgentConfigurationsService
    {
        /// <summary>
        /// Добавить провайдер динмической конфигураций, для последующего использование в транспортных ответах
        /// </summary>
        void AddWithKeyOrThrow(IAgentDynamicConfigurationProvider provider);

        /// <summary>
        /// сформировать данные конфигураций для транспортного ответа
        /// </summary>
        IList<ConfigurationResponse> GetConfigurationResponses(MessageHeaders messageHeaders, IReadOnlyCollection<ConfigurationRequestDataItem> configurationsOnAgent);
    }
}
