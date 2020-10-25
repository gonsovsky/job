using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ClientShaper: Part
    {
        public List<Part> Parts;

        public override string Name { get; set; } = "Клиент";

        public ClientShaper()
        {
            Parts = new List<Part>()
            {
                new ClientShaperServer(){SingleRequesed = true, Name = Name + ".ШейпСрв"},
                new ClientShaperClient(){Name = Name + ".ШейпКли"}
            };
            ((ClientShaperServer) Parts[0]).ClientPartner = ((ClientShaperClient) Parts[1]);
        }
    }
}
