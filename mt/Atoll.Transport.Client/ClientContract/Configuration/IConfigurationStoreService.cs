using System;
using System.Collections.Generic;
using System.IO;

namespace Atoll.Transport.Client.Contract
{

    /// <summary>
    /// хранилище конфигураций агента
    /// </summary>
    public interface IConfigurationStoreService
    {
        IList<ConfigurationState> GetStateItems();
        void Save(ConfigurationPart configurationPart);

        void Subscribe(IConfigurationUpdateSubscriber subscriber);
        void UnSubscribe(IConfigurationUpdateSubscriber subscriber);
    }
}
