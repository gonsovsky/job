using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ServerShaperClient: Client
    {
        public string ServerContent = "";
        public override void Serve()
        {
            while (ServerContent == "")
            {
                Thread.Sleep(Delay);
            }
            DoSend(ServerContent);
        }
    }
}
