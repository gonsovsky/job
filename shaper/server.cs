using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace shaper
{
    public class Server: Part
    {
        public override string Name { get; set; } = "Сервер";

        public string Request;

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
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();

                HttpListenerRequest request = context.Request;

                using (Stream receiveStream = request.InputStream)
                {
                    using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                    {
                        Request = readStream.ReadToEnd();
                    }
                }
                Log($"{MyName} <-- {PrevName}: {Request}");

                HttpListenerResponse response = context.Response;

                string responseString = "OK";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                //Log($"{MyName} --> {PrevName}: {responseString}");
                break;
                ;
            }
            listener.Stop();
        }
    }
}
