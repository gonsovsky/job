using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebApplication18.Transport
{
    public class PacketPart: PacketPartInfo
    {
        [BsonElement("data")]
        public byte[] Bytes { get; set; }

        public AgentInfo AgentInfo { get; set; }
    }

    public class Lease
    {
        [BsonId]
        public string Id { get; set; }

        //[BsonId]
        public string ClientId { get; set; }

        [BsonElement("st")]
        [BsonIgnoreIfNull]
        public DateTime? UpdateTime { get; set; }
    }

    public class AgentInfo
    {
        [BsonElement("cd")]
        public string Domain { get; set; }

        [BsonElement("cn")]
        public string ComputerName { get; set; }
    }

    public class PacketPartInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("pkid")]
        public string PacketId { get; set; }

        [BsonElement("pkey")]
        public string ProviderKey { get; set; }

        [BsonElement("isf")]
        public bool IsFinal { get; set; }

        [BsonElement("sp")]
        public int StartPosition { get; set; }

        [BsonElement("ep")]
        public int EndPosition { get; set; }

        [BsonElement("ppt")]
        [BsonIgnoreIfNull]
        public string PreviousPartStorageToken { get; set; }

        [BsonElement("ppid")]
        [BsonIgnoreIfNull]
        public string PreviousPartId { get; set; }

        [BsonElement("ftt")]
        [BsonIgnoreIfNull]
        public DateTime? FinalPartTransferTime { get; set; }

        [BsonElement("dpuid")]
        [BsonIgnoreIfNull]
        public string ProcessingDataId { get; set; }

        ///<remarks>обновляется для реализации Lease</remarks>
        [BsonElement("st")]
        [BsonIgnoreIfNull]
        public DateTime? StartTime { get; set; }

        [BsonElement("et")]
        [BsonIgnoreIfNull]
        public DateTime? EndTime { get; set; }
    }

}
