using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Atoll.Transport.ServerBundle
{
    public class Lease
    {
        [BsonId]
        public string Id { get; set; }

        //[BsonId]
        [BsonElement("cl")]
        [BsonIgnoreIfNull]
        public string ClientId { get; set; }

        [BsonElement("st")]
        [BsonIgnoreIfNull]
        public DateTime? UpdateTime { get; set; }
    }

}
