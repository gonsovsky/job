using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace shaper
{
    public static class Parts
    {
        public static void LinkAll(this List<Part> parts)
        {
            for (int i = plain.Count - 1; i >= 0; i--)
                plain[i].Mother = plain;
            var servers = plain.Where(a => a is Server).ToArray();
            var clients = plain.Where(a => a is Client).ToArray();
            for (int i = 0; i <= servers.Count() - 1; i++)
                servers[i].MyPort = Part.StartPort + i;
            for (int i = 0; i <= clients.Count() - 1; i++)
                clients[i].MyPort = Part.StartPort + i;
        }

        public static void StartAll(this List<Part> parts)
        {
            foreach (var x in parts.Where(a => a is Server))
                new Thread(x.Serve).Start();

            Thread.Sleep(Part.Delay);

            foreach (var x in parts.Where(a => a is Client))
                new Thread(x.Serve).Start();
        }

        public static List<Part> Plain(this List<Part> parts)
        {
            var res = new List<Part>() { };
            foreach (var x in parts)
            {
                if (x is ClientShaper)
                {
                    foreach (var y in ((Shaper) x).Parts)
                        res.Add(y);
                }
                else res.Add(x);
            }

            return res;
        }
    }
}
