using System;
using System.Collections.Generic;
using Atoll.Transport.Client.Contract;

namespace Atoll.Transport.Client.Bundle
{
    public class SendResult
    {
        public bool IsSended { get; private set; }
        public TimeSpan? TimeoutToNextTry { get; private set; }
        public IList<ITransportPacketInfo> IgnoredPackets { get; private set; }
        public IList<TransferedPacketStats> TransferedPackets { get; private set; }
        public bool ServerDbChanged { get; private set; }

        public SendResult(bool isSuccess, bool serverDbChanged, IList<TransferedPacketStats> transferedPackets, IList<ITransportPacketInfo> ignoredPackets, TimeSpan? timeoutToNextTry)
        {
            this.IsSended = isSuccess;
            this.TransferedPackets = transferedPackets;
            this.TimeoutToNextTry = timeoutToNextTry;
            this.IgnoredPackets = ignoredPackets;
        }

        public static SendResult Success(bool serverDbChanged, IList<TransferedPacketStats> transferedPackets, IList<ITransportPacketInfo> ignoredPackets)
        {
            return new SendResult(true, serverDbChanged, transferedPackets, ignoredPackets, null);
        }

        public static SendResult Retry(long nextTryMilliseconds)
        {
            return new SendResult(false, false, new List<TransferedPacketStats>(), new List<ITransportPacketInfo>(), TimeSpan.FromMilliseconds(nextTryMilliseconds));
        }

        public static SendResult Retry(TimeSpan nextTry)
        {
            return new SendResult(false, false, new List<TransferedPacketStats>(), new List<ITransportPacketInfo>(), nextTry);
        }
    }
}
