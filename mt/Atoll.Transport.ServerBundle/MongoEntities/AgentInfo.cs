using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Atoll.Transport.ServerBundle
{
    public class AgentIdentity
    {
        [BsonElement("cd")]
        public string DomainName { get; set; }

        [BsonElement("cn")]
        public string ComputerName { get; set; }
    }

}
