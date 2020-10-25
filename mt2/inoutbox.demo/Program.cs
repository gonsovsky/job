﻿using System;
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
            var outbox = storage.GetOutBox(
                Config.QueueName, 1);
            ((OutBox)outbox).Init(Config.StorageFolder, Config.ConStr, Config.DbFile);

            var transportMongo = new TransportMongo.TransportMongo(
                Config.TransportFolder,
                Config.TransportUrl
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
