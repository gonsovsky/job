using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace shaper
{
    public class Client : Part
    {
        public override string Name { get; set; } = "Клиент";

        protected bool _sent;

        public override async void Serve()
        {
            DoSend(Program.BaseContent);
        }

        protected async void DoSend(string data, int packetNo =0, int totalPacket=0)
        {
            if (_sent)
                return;
            if (packetNo != 0)
            {
                Log($"{MyName} --> {NextName}: {data} [packet: {packetNo}/{totalPacket}]");

            }
            else
                Log($"{MyName} --> {NextName}: {data}");
            try
            {
                var client = new HttpClient
                {
                    Timeout = new TimeSpan(0, 0, 0,Part.Delay)
                };
                var response = await client.PostAsync($"http://localhost:{MyPort}?packetNo={packetNo}&packetTotal={totalPacket}",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                var contents = await response.Content.ReadAsStringAsync();
                _sent = true;
                //Log($"{MyName} <-- {NextName}: {contents}");
                return;
            }
            catch (Exception e)
            {
              //  Console.WriteLine(e.Message);
                this.Serve();
            }
        }
    }
}
