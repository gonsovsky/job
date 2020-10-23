using Atoll.Transport.ServerBundle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Atoll.Transport.DataProcessing
{
    public class ReservationRangeLimits
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public ReservationRangeLimits ForAlreadyReserved(int alreadyReserved)
        {
            return new ReservationRangeLimits
            {
                Min = this.Min - alreadyReserved,
                Max = this.Max - alreadyReserved,
            };
        }
    }

    public class DataProcessingContext
    {
        public bool HasDataToProcess
        {
            get
            {
                return this.ReservedRange?.HasData == true;
            }
        }

        public ReservedRange ReservedRange { get; set; }

        public int RangeRecordsCount()
        {
            return this.ReservedRange?.RecordsCount() ?? 0;
        }

        public bool IsRangeFilled()
        {
            return this.NeedReserveRange.Min < this.RangeRecordsCount();
        }

        public ReservationRangeLimits NeedReserveRange { get; set; }
        public string DpuId { get; set; }
        public IEnumerable<DataNodeDefinition> OrderedNodes { get; set; }
        public CancellationToken CancellationToken { get; set; }
        public IDictionary<AgentPacketPartInfo, List<PacketPartNodePair>> PacketToChain { get; set; }
        public bool HasDataInPreviousProcessing { get; set; }
        public int LoadedInMemoryBinaryPartsBatchSize { get; set; }

        public DataProcessingContext()
        {
            this.OrderedNodes = Enumerable.Empty<DataNodeDefinition>();
            this.PacketToChain = new Dictionary<AgentPacketPartInfo, List<PacketPartNodePair>>();
            this.LoadedInMemoryBinaryPartsBatchSize = 1;
            this.NeedReserveRange = new ReservationRangeLimits
            {
                Min = 15,
                Max = 21
            };
        }
    }


    public class PacketPartNodePair
    {

        public PacketPartInfo PacketPartInfo { get; set; }

        public DataNodeDefinition NodeDefinition { get; set; }

    }
}
