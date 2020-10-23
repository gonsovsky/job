using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using WebApplication18.Transport;

namespace WebApplication18
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Delay(2000)
                .ContinueWith((t) =>
                {
                    //Task.Factory.StartNew(TestTransport, TaskCreationOptions.LongRunning);
                });
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        //public static void TestTransport()
        //{
        //    var data = new List<Dictionary<string, object>>()
        //    {
        //        new Dictionary<string, object>
        //        {
        //            {"fileName", "testFile1.txt" }
        //        },
        //        new Dictionary<string, object>
        //        {
        //            {"fileName", "testFile2.txt" }
        //        }
        //    };

        //    ITransportClient transportClient = null;
        //    var url = "http://localhost:52932/tests/transport/exchange";
        //    //var serializer = new JsonSerializer();
        //    using (var session = transportClient.CreateSession("files"))
        //    {
        //        bool needCommit = true;
        //        if (data.Any())
        //        {
        //            var packet = session.CreatePacket();

        //            foreach (var item in data)
        //            {
        //                var itemStr = JsonConvert.SerializeObject(item);
        //                var bytes = Encoding.UTF8.GetBytes(itemStr);
        //                if (!packet.TryWrite(bytes))
        //                {
        //                    break;
        //                }

        //                if (packet.Length < 1000)
        //                {
        //                    // кусок не входит в пакет
        //                    needCommit = false;
        //                    return;
        //                    //session.Dispose();
        //                }
        //            }

        //            session.Add(packet);
        //        }

        //        if (needCommit)
        //        {
        //            session.Commit();
        //        }
        //    }

        //    RunAfter(3000, TestTransport);
        //}

        private static void RunAfter(int ms, Action action)
        {
            Task.Delay(ms).ContinueWith(t => action());
        }

        private static void TestJsonReader()
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(""));
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    Console.WriteLine("Token: {0}, Value: {1}", reader.TokenType, reader.Value);
                }
                else
                {
                    Console.WriteLine("Token: {0}", reader.TokenType);
                }
            }
        }
    }
}
