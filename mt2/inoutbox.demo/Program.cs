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
            var svcStorage = new StorageSvc();
            var outbox = svcStorage.GetOutBox(
                ProgramConfig.QueueName, ProgramConfig.DefaultPriority);
            outbox.Init(
                new Config()
                {
                    StorageFolder = ProgramConfig.StorageFolder,
                    ConStr = ProgramConfig.ConStr,
                    DbFile = ProgramConfig.DbFile
                });

            var transportMongo = new TransportMongo.TransportMongo(
                ProgramConfig.TransportFolder,
                ProgramConfig.TransportUrl
            );

            transportMongo.OnSent += (int itemId) =>
            {
                outbox.Send(new OutItem(){Id=itemId});
                Console.Write($"Sent items:{string.Join(",", outbox.Sent().Select(x => x.Id.ToString()))}\n");
            };

            outbox.OnAddItem += (string queue, IOutItem item) =>
            {
                Console.Write($"Outgoing items:{string.Join(",", outbox.Unsent().Select(x => x.Id.ToString()))}\n");
                var itemStream = outbox.Read(item);
                transportMongo.SendMessage(item.Id, queue, itemStream);
            };

            var newItem = outbox.Add("-");
            try
            {
                using (var msgStream = outbox.AddWrite(newItem))
                {
                    using (var biosStream = SmBiosExtractor.OpenRead())
                    {
                        biosStream.CopyTo(msgStream);
                    }
                }
                outbox.AddCommit(newItem);
            }
            catch (Exception)
            {
                outbox.AddRollback(newItem);
            }

            Console.ReadLine();
        }
    }
}
