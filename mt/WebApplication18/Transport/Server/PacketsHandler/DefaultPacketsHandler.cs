//using System.Collections.Concurrent;
//using System.Collections.Generic;

//namespace WebApplication18.Transport
//{
//    public class DefaultPacketsHandler : IPacketsHandler
//    {
//        private IDictionary<string, IPacketProcessor> packetsProcessors = new ConcurrentDictionary<string, IPacketProcessor>();

//        public void AddWithKeyOrThrow(IPacketProcessor processor)
//        {
//            this.packetsProcessors.Add(processor.ProviderKey, processor);
//        }

//        public PacketProcessingResult Process(PacketData packetData)
//        {
//            if (!packetsProcessors.TryGetValue(packetData.ProviderKey, out var processor))
//            {
//                return PacketProcessingResult.NoProcessor;
//            }

//            var response = processor.Handle(packetData);

//            return response;
//        }
//    }
//}
