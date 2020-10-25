using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace shaper
{
    public static class Parts
    {
        public static List<Part> LinkAll(this List<Part> parts)
        {
            var res = new List<Part>() { };
            foreach (var x in parts)
            {
                if (x is ClientShaper shaper)
                {
                    res.AddRange(shaper.Parts);
                } else
                if (x is ServerShaper shaperS)
                {
                    res.AddRange(shaperS.Parts);
                }
                else res.Add(x);
            }

            for (int i = res.Count - 1; i >= 0; i--)
                res[i].Mother = res;
            var servers = res.Where(a => a is Server).ToArray();
            var clients = res.Where(a => a is Client).ToArray();
            for (int i = 0; i <= servers.Count() - 1; i++)
                servers[i].MyPort = Part.StartPort + i;
            for (int i = 0; i <= clients.Count() - 1; i++)
                clients[i].MyPort = Part.StartPort + i;
            Part.MaxName = res.OrderByDescending(x => x.MyName.Length).First().MyName.Length;

            return res;
        }

        public static void StartAll(this List<Part> parts)
        {
            foreach (var x in parts.Where(a => a is Server))
                new Thread(x.Serve).Start();

            Thread.Sleep(Part.Delay);

            foreach (var x in parts.Where(a => a is Client))
                new Thread(x.Serve).Start();
        }

    }
}
