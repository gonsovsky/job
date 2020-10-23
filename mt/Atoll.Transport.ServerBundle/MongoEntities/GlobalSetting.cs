using MongoDB.Bson.Serialization.Attributes;

namespace Atoll.Transport.ServerBundle
{
    public class GlobalSetting
    {
        [BsonId]
        public string Id { get; set; }

        public string Token { get; set; }
    }

}
