using System;
using System.Collections.Generic;
using System.Linq;
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
                    var str = ($"#{N + 1}[{MyPort}] {Name}");
                return str.PadRight(MaxName, ' ');
            }
        }

        public static int NL = 0;

        public int MyPort;
        public static int MaxName;
        public virtual string Name { get; set; }
        public static int Delay = 1;
        public static int StartPort = 6000;
        public int Split = 3;
        public virtual async void Serve(){}

        public void Log(string s)
        {
            NL++;
            Console.WriteLine($"{NL.ToString().PadLeft(2,'0')}> {s}");
            Thread.Sleep(Delay);

        }
    }
}
