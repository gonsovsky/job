using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace shaper
{
    public abstract class Part
    {
        public List<Part> Mother;

        public int N => Mother.IndexOf(this);

        public virtual string NextName => Mother[N + 1].MyName;

        public virtual string PrevName => Mother[N - 1].MyName;

        public virtual string MyName
        {
            get
            {
               var str= ($"#{N + 1}[{MyPort}] {Name}");
               return str.PadRight(24, ' ');
            }
        }


        public int MyPort;

        public virtual string Name { get; set; }
        public static int Delay = 500;
        public static int StartPort = 6000;
        public string BaseContent = "1234567890";
        public virtual async void Serve(){}

        public void Log(string s)
        {
            Console.WriteLine(s);
            Thread.Sleep(Delay);
        }
    }
}
