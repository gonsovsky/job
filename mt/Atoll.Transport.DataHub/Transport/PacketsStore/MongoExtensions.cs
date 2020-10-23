using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;

namespace Atoll.Transport.DataHub
{
    public static class MongoExtensions
    {
        public static bool Ping(this IMongoDatabase database, CancellationToken token, int? maxPingTimeoutMs = null)
        {
            //https://stackoverflow.com/questions/28835833/how-to-check-connection-to-mongodb
            //https://stackoverflow.com/questions/30713599/mongodb-driver-2-0-c-sharp-is-there-a-way-to-find-out-if-the-server-is-down-in
            try
            {
                bool isMongoLive = database.RunCommandAsync((Command<BsonDocument>)"{ping:1}", null, token).Wait(maxPingTimeoutMs ?? 1000, token);
                return isMongoLive;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

}
