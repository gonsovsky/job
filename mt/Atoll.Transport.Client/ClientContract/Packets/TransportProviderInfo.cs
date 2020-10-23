using System;
using System.Collections.Generic;

namespace Atoll.Transport.Client.Contract
{
    public class DelegateTransportProviderInfo : ITransportProviderInfo
    {
        private readonly Func<IEnumerable<ITransportPacketInfo>> getPacketInfos;
        public DelegateTransportProviderInfo(string provider, Func<IEnumerable<ITransportPacketInfo>> getPacketInfos)
        {
            this.ProviderKey = provider;
            this.getPacketInfos = getPacketInfos;
        }

        public string ProviderKey { get; private set; }

        public IEnumerable<ITransportPacketInfo> GetPackets()
        {
            return this.getPacketInfos();
        }
    }
}
