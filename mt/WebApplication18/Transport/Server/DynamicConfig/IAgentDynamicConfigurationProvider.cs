namespace WebApplication18.Transport
{
    /// <summary>
    /// провайдер динамической конфигурации для агентов
    /// </summary>
    public interface IAgentDynamicConfigurationProvider
    {
        /// <summary>
        /// Ключ провайдера
        /// </summary>
        string ProviderKey { get; }

        /// <summary>
        /// возвращает конфигурационную информацию по данным агента
        /// </summary>
        AgentDynamicConfiguration GetConfigurationData(AgentIdentifierData idData);
    }
}
