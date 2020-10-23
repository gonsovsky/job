using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication18.Transport
{
    public class GlobalSetting
    {
        [BsonId]
        public string Id { get; set; }

        public string Token { get; set; }
    }

}
