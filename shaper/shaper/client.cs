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
            DoSend(BaseContent);
        }

        protected async void DoSend(string data)
        {
            if (_sent)
                return;
            Log($"{MyName} --> {NextName}: {data}");
            try
            {
                var client = new HttpClient
                {
                    Timeout = new TimeSpan(0, 0, 1)
                };
                var response = await client.PostAsync($"http://localhost:{MyPort}",
                    new StringContent(data, Encoding.UTF8, "application/json"));
                var contents = await response.Content.ReadAsStringAsync();
                _sent = true;
                //Log($"{MyName} <-- {NextName}: {contents}");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                this.Serve();
            }
        }
    }
}
