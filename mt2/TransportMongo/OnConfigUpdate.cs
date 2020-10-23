using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Atoll.Transport.Client.Contract;

namespace TransportMongo
{
    class TestOnConfigUpdate : IConfigurationUpdateSubscriber
    {
        public string ProviderKey => "test";

        public void OnUpdate(Func<Stream> configStreamFactory)
        {
            Console.WriteLine("On config update");
            Console.WriteLine("ProviderKey: " + ProviderKey);
            Console.WriteLine("Config -");
            using (var stream = configStreamFactory())
            using (var streamReader = new StreamReader(stream))
            {
                Console.WriteLine(streamReader.ReadToEnd());
            }
        }
    }
}
