using System;
using System.IO;

namespace ClassLibrary1.Transport
{
    public interface IConfigurationUpdateSubscriber
    {
        string ProviderKey { get; }
        void OnUpdate(Func<Stream> configStreamFactory);
    }
}
