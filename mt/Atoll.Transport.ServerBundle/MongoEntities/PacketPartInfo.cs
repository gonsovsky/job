using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Atoll.Transport.ServerBundle
{
    /// <remarks>при переносе свойства из однго класса в другой некоторые запросы mongo могут сломаться - нужно править Projection часть запроса, например выражения exclude)</remarks>
    public class PacketPartInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("pkid")]
        public string PacketId { get; set; }

        [BsonElement("pkey")]
        public string ProviderKey { get; set; }

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

        //[BsonElement("dpuid")]
        //[BsonIgnoreIfNull]
        //public string ProcessingUnitId { get; set; }

        /////<remarks>обновляется для реализации Job-ов и Lease</remarks>
        //[BsonElement("st")]
        //[BsonIgnoreIfNull]
        //public DateTime? StartTime { get; set; }

        //[BsonElement("et")]
        //[BsonIgnoreIfNull]
        //public DateTime? EndTime { get; set; }
    }

}
