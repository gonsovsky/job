//using Newtonsoft.Json;
//using System;
//using System.Diagnostics;
//using System.IO;

//namespace WebApplication18.Transport
//{
//    public abstract class PacketProcessorBase : IPacketProcessor
//    {
//        public abstract string ProviderKey { get; }

//        public abstract PacketProcessingResult Handle(PacketData data);

//    }

//    public class TestPacketProcessor: PacketProcessorBase
//    {
//        public TestPacketProcessor(string providerKey)
//        {
//            this.ProviderKey = providerKey;
//        }

//        public override string ProviderKey { get; }

//        public override PacketProcessingResult Handle(PacketData data)
//        {
//            using (var stream = data.GetStream())
//            {
//                try
//                {
//                    var serializer = new JsonSerializer();

//                    using (var sr = new StreamReader(stream))
//                    using (var jsonTextReader = new JsonTextReader(sr))
//                    {
//                        var val = serializer.Deserialize(jsonTextReader);
//                        Console.WriteLine("deserialized value, type - " + val?.ToString());
//                    }
//                }
//                catch (System.Exception)
//                {
//                    // ignore
//                }
//            }

//            return PacketProcessingResult.Success;
//        }
//    }
//}
