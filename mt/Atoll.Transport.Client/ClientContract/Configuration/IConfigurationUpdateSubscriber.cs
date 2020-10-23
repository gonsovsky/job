using System;
using System.IO;

namespace Atoll.Transport.Client.Contract
{
    public interface IConfigurationUpdateSubscriber
    {
        string ProviderKey { get; }
        void OnUpdate(Func<Stream> configStreamFactory);
    }
}
