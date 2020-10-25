﻿using System;
using System.Collections.Generic;
using System.Text;

namespace shaper
{
    public class ClientShaperServer: Server
    {
        public ClientShaperClient ClientPartner;

        public override void Serve()
        {
            base.Serve();
            Log($"{MyName} --> {NextName}: {Request}");
            ClientPartner.SendPartial(Request);
        }
    }
}
