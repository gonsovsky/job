using System;
using System.Data;
using System.IO;
using InOutBox.Implementation;
using InOutBox.Contracts;
using SmBios.Extractor;
using TransportMongo;

namespace InOutBox.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            var storage = new Storage();
            var outbox = storage.GetOutBox(
                Config.QueueName, Config.StorageFolder, Config.ConStr, Config.DbFile);

            var transportMongo = new TransportMongo.TransportMongo(
                Config.TransportFolder,
                Config.TransportUrl
            );

            var msgId = outbox.Add("-");
            try
            {
                using (var msgStream = outbox.Write(msgId))
                {
                    using (var biosStream = SmBiosExtractor.OpenRead())
                    {
                        biosStream.CopyTo(msgStream);
                    }
                }
                outbox.Commit(msgId);
            }
            catch (Exception e)
            {
                outbox.Rollback(msgId);
            }
        }
    }
}
