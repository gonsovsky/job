using System;
using System.Collections.Generic;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class PacketSizeCalculatorUtil
    {

        public static int CalculateMessageSize(SendMessageSizeLimits packetSizeLimits, IList<ITransportPacketInfo> packetInfos)
        {
            // пока свожу к уравнению d * x + z * y = 11
            // z between min and max
            // d between min and max
            // TODO реализовать решение
            var size = 0;
            foreach (var item in packetInfos)
            {
                var nextPacketSize = size + item.Length;
                if (nextPacketSize > packetSizeLimits.Min)
                {
                    //size = nextPacketSize;
                    //return size;
                    if (nextPacketSize < packetSizeLimits.Max)
                    {

                    }
                    else
                    {
                        return size;
                    }
                }
                else
                {
                    size = nextPacketSize;
                }
            }
            return size;
        }

    }
}
