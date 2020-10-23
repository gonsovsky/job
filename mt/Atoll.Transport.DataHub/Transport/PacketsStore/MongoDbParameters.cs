namespace Atoll.Transport.DataHub
{
    public class MongoDbParameters
    {
        public string MongoConnString { get; set; }
        public string DbName { get; set; }

        public MongoDbParameters(string mongoConnString, string dbName)
        {
            this.MongoConnString = mongoConnString;
            this.DbName = dbName;
        }
    }

}
