using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ClientShaperClient: Client
    {
        public string ServerContent = "";

        public override async void Serve()
        {
            while (ServerContent == "")
            {
                Thread.Sleep(Delay);
            }
            DoSend(ServerContent);
        }

        public void SendPartial(string content)
        {
            ServerContent = content;
        }
    }
}
