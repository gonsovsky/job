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
        public static string BaseContent = "ABCDEFGHJ";

        static void Main(string[] args)
        {
            //BaseContent = System.IO.File.ReadAllText("C://_temp2/1.txt");

            List<Part> parts = new List<Part>()
            {
                new Client(),
                new ClientShaper(),
                new ServerShaper(),
                new Server()
            };
            parts = parts.LinkAll();
            foreach (var x in parts)
                Console.WriteLine(x.Name + " " + x.MyPort);
            Console.WriteLine("");
            parts.StartAll();
            Console.ReadLine();
        }
    }
}
