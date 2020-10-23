using System;
using System.IO;

namespace Atoll.Transport.DataHub
{
    /// <summary>
    /// Данные о динамической конфигурации агента
    /// </summary>
    public class AgentDynamicConfiguration
    {
        /// <summary>
        /// Токен конфигурации (пока предполагаю что токен будет использоваться для проверки что конфигурация изменилась)
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// фабрика для создания потока конфигурации
        /// </summary>
        public Func<Stream> StreamFactory { get; set; }
    }
}
