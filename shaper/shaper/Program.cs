using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace shaper
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Part> parts = new List<Part>()
            {
                new Client(),
                new ClientShaper(),
                //new ServerShaper(),
                new Server()
            };
            parts = parts.Plain();
            parts.LinkAll();
            foreach (var x in parts.Plain())
                Console.WriteLine(x.Name + " " + x.MyPort);
            parts.StartAll();
            Console.ReadLine();
        }
    }
}
