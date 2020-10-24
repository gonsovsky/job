using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
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
            var outbox = (OutBox)storage.GetOutBox(
                Config.QueueName);
            outbox.Init(Config.StorageFolder, Config.ConStr, Config.DbFile);

            var transportMongo = new TransportMongo.TransportMongo(
                Config.TransportFolder,
                Config.TransportUrl
            );

            transportMongo.OnSent += (int itemId) =>
            {
                outbox.Send(itemId);
                Console.Write($"Sent items:{string.Join(",", outbox.Sent().Select(x => x.ToString()))}\n");
            };

            outbox.OnNewItem += (string queue, int itemId) =>
            {
                Console.Write($"Outgoing items:{string.Join(",", outbox.Unsent().Select(x => x.ToString()))}\n");
                var itemStream = outbox.Read(itemId);
                transportMongo.SendMessage(itemId, queue, itemStream);
            };

            var newId = outbox.Add("-");
            try
            {
                using (var msgStream = outbox.AddWrite(newId))
                {
                    using (var biosStream = SmBiosExtractor.OpenRead())
                    {
                        biosStream.CopyTo(msgStream);
                    }
                }
                outbox.AddCommit(newId);
            }
            catch (Exception)
            {
                outbox.AddRollback(newId);
            }

            Console.ReadLine();
        }
    }
}
