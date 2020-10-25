using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ClientShaperClient: Client
    {
        public string ServerContent = "";

        public bool sent = false;

        public override async void Serve()
        {
            while (ServerContent == "")
            {
                Thread.Sleep(Delay);
            }

            var parts = (int)(ServerContent.Length / Split);
            for (int i = 1; i <= parts; i++)
            {
                var q = i;
                var z = parts;
                var str = string.Join("", ServerContent.Skip((i - 1) * Split).Take(Split));
                DoSend(str, q, z);
            }

            ServerContent = "";
            sent = true;

        }

        public void SendPartial(string content)
        {
            sent = false;
            ServerContent = content;
            while (sent == false)
            {
                Thread.Sleep(Delay);
            }
        }
    }
}
