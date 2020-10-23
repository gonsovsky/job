using System;

namespace Atoll.Transport.ServerBundle
{
    // с исполнением eval не стал разбираться - Command eval failed: no such command: 'eval'.
    //public static class MongoClientExtensions
    //{
    //    /// <summary>
    //    /// Evaluates the specified javascript within a MongoDb database
    //    /// </summary>
    //    /// <param name="database">MongoDb Database to execute the javascript</param>
    //    /// <param name="javascript">Javascript to execute</param>
    //    /// <returns>A BsonValue result</returns>
    //    public static async Task<BsonValue> EvalAsync(this IMongoDatabase database, string javascript)
    //    {
    //        var client = database.Client as MongoClient;

    //        if (client == null)
    //            throw new ArgumentException("Client is not a MongoClient");

    //        var function = new BsonJavaScript(javascript);
    //        var op = new EvalOperation(database.DatabaseNamespace, function, null);

    //        using (var writeBinding = new WritableServerBinding(client.Cluster, new CoreSessionHandle(NoCoreSession.Instance)))
    //        {
    //            return await op.ExecuteAsync(writeBinding, CancellationToken.None);
    //        }
    //    }
    //}

    public interface IDbServicesFactory
    {
        ITimeService GetTimeService(string connString, string databaseName, TimeSpan updateTimeInterval);
        //ILockService GetLeaseLockService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null);
        IDbService GetDbService(string connString, string databaseName, TimeSpan leaseCheckTimeout, TimeSpan leaseLostTimeout, TimeSpan? updateTimeInterval = null);
    }

}
