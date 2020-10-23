using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Atoll.Transport.ServerBundle
{
    /// <remarks>при переносе свойства из однго класса в другой некоторые запросы mongo могут сломаться - нужно править Projection часть запроса, например выражения exclude)</remarks>
    public class PacketPart: AgentPacketPartInfo
    {

        [BsonElement("data")]
        public byte[] Bytes { get; set; }

        //[BsonElement("c")]
        //public AgentIdentity AgentInfo { get; set; }

        [BsonElement("dpuid")]
        [BsonIgnoreIfNull]
        public string ProcessingUnitId { get; set; }

        ///<remarks>обновляется для реализации Job-ов и Lease</remarks>
        [BsonElement("st")]
        [BsonIgnoreIfNull]
        public DateTime? StartTime { get; set; }

        [BsonElement("et")]
        [BsonIgnoreIfNull]
        public DateTime? EndTime { get; set; }

        [BsonElement("ct")]
        [BsonIgnoreIfNull]
        public DateTime CreatedTime { get; set; }
    }

    /// <remarks>при переносе свойства из однго класса в другой некоторые запросы mongo могут сломаться - нужно править Projection часть запроса, например выражения exclude)</remarks>
    public class AgentPacketPartInfo : PacketPartInfo
    {

        [BsonElement("c")]
        public AgentIdentity AgentInfo { get; set; }

    }
}
