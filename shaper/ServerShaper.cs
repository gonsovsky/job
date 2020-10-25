using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ServerShaper: Part
    {
        public List<Part> Parts;

        public override string Name { get; set; } = "Сервер";

        public ServerShaper()
        {
            Parts = new List<Part>()
            {
                new ServerShaperServer(){SingleRequesed = false, Name = this.Name + ".ШейпСрв"},

                new ServerShaperClient(){ Name = this.Name + ".ШейпКли"},
            };
            ((ServerShaperServer) Parts[0]).ClientPartner = ((ServerShaperClient) Parts[1]);
        }
    }
}
