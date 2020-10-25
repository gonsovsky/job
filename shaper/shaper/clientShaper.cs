using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shaper
{
    public class ClientShaper: Part
    {
        public List<Part> Parts;

        protected ClientShaper()
        {
            Parts = new List<Part>()
            {
                new ClientShaperServer(){Name = Name + ".Приемник"},
                new ClientShaperClient(){Name = Name + ".Отдатчик"}
            };
            ((ClientShaperServer) Parts[0]).ClientPartner = ((ClientShaperClient) Parts[1]);
        }
    }
}
