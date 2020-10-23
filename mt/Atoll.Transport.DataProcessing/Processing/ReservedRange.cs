using Atoll.Transport.ServerBundle;
using System.Collections.Generic;
using System.Linq;

namespace Atoll.Transport.DataProcessing
{
    public class ReservedRange
    {
        public bool HasData
        {
            get
            {
                return this.DataNodeToFinalParts?.Values.Any(x => x.Any()) == true;
            }
        }

        public int RecordsCount()
        {
            return this.DataNodeToFinalParts.Values.Sum(x => x.Count);
        }

        public IDictionary<DataNodeDefinition, List<AgentPacketPartInfo>> DataNodeToFinalParts { get; set; }

        public ReservedRange()
        {
            this.DataNodeToFinalParts = new Dictionary<DataNodeDefinition, List<AgentPacketPartInfo>>();
        }

        public void Add(ReservedRange range)
        {
            if (range != null)
            {
                foreach (var item in range.DataNodeToFinalParts)
                {
                    if (!this.DataNodeToFinalParts.TryGetValue(item.Key, out var list))
                    {
                        list = new List<AgentPacketPartInfo>();
                        this.DataNodeToFinalParts.Add(item.Key, list);
                    }
                    list.AddRange(item.Value);
                }
            }
        }
    }
}
