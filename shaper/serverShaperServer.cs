using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ServerShaperServer: Server
    {
        public Dictionary<int, string> Data=new Dictionary<int, string>();

        public ServerShaperClient ClientPartner;

        public override void Serve()
        {
            var prefixes = new List<string>() { $"http://localhost:{MyPort}/" };

            HttpListener listener = new HttpListener();

            foreach (string s in prefixes)
            {
                listener.Prefixes.Add(s);
            }
            listener.Start();

            while (true)
            {
                HttpListenerContext context = listener.GetContext();

                HttpListenerRequest request = context.Request;

                var q = request.RawUrl.Contains("first=true");

                var d = System.Web.HttpUtility.ParseQueryString(request.RawUrl);
                var packetNo = int.Parse(d["/?packetNo"]);
                var totalPacket = int.Parse(d["packetTotal"]);

                using (Stream receiveStream = request.InputStream)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        Request = readStream.ReadToEnd();
                    }
                }

                Data[packetNo] = Request;

                Log($"{MyName} <-- {PrevName}: {Request} [packet: {packetNo}/{totalPacket}]");

                HttpListenerResponse response = context.Response;

                string responseString = "OK";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                //Log($"{MyName} --> {PrevName}: {responseString}");
                if (SingleRequesed)
                    break;

                if (Data.Count == totalPacket)
                {
                    var finalData = "";
                    Data.OrderBy(x => x.Key);
                    foreach (var value in Data.Values)
                    {
                        finalData += value;
                    }
                    Log($"{MyName} --> {NextName}: {finalData}");
                    ClientPartner.ServerContent = finalData;
                    break;
                }
            }
            listener.Stop();
        }
    }
}
