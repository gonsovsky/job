using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
//using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestUploadReadHeaders
{
    class Program
    {
        private static bool readBody = false;
        private static string url = "http://localhost:52932/tests/readonlyheaders/post" + (readBody ? "?readbody=true" : "");
        //private static string url = "http://localhost:61447/tests/readonlyheaders/post" + (readBody? "?readbody=true" : "");
        static void Main(string[] args)
        {
            SendWebRequest();
        }

        static void SendWebRequest()
        {
            var mbCount = 1000 * 1000;
            var bytesFotMb = Enumerable.Range(0, 1000).Select(x => (byte)(x % 5)).ToArray();

            var stopwatch = Stopwatch.StartNew();
            var req = WebRequest.Create(url) as HttpWebRequest;
            //var req = WebRequest.Create(url);
            //req.KeepAlive = true;
            req.AllowWriteStreamBuffering = false;
            //req.KeepAlive = false;
            req.Method = "POST";
            req.SendChunked = true;
            //req.Credentials = new NetworkCredential(user.UserName, user.UserPassword);
            //req.PreAuthenticate = true;

            using (Stream requestStream = req.GetRequestStream())
            {
                for (int i = 0; i < mbCount; i++)
                {
                    requestStream.Write(bytesFotMb, 0, bytesFotMb.Length);
                }
            }

            try
            {
                var response = req.GetResponse();
                using (var resStream = response.GetResponseStream())
                using (var streamReader = new StreamReader(resStream))
                {
                    Console.WriteLine("response-{0}", streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                var wRespStatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                if ((int)wRespStatusCode == 429)
                {
                    var timeout = int.TryParse(ex.Response.Headers.GetValues("Retry-After").FirstOrDefault(), out var temp) ? temp : 5 * 60 * 1000;
                    Console.WriteLine("Retry-After-{0}", timeout);
                }
            }

            stopwatch.Stop();

            Console.WriteLine("elapsed-{0}", stopwatch.Elapsed);

            Console.ReadKey();
        }

        static void SendWebClient()
        {
            var mbCount = 1000 * 1000;
            var bytesFotMb = Enumerable.Range(0, 1000).Select(x => (byte)(x % 5)).ToArray();

            var wc = new WebClient();
            using (var stream = wc.OpenWrite(url))
            {
                for (int i = 0; i < mbCount; i++)
                {
                    stream.Write(bytesFotMb, 0, bytesFotMb.Length);
                }
            }
        }

        //static void SendHttpClient()
        //{
        //    var mbCount = 1000 * 1000;
        //    var bytesFotMb = Enumerable.Range(0, 1000).Select(x => (byte)(x % 5)).ToArray();

        //    var httpClient = new HttpClient();
        //    var stopwatch = Stopwatch.StartNew();
        //    // https://github.com/dotnet/corefx/issues/9071
        //    //var response = httpClient.PostAsync(url, new WriteToStreamContent((stream)=>
        //    //{
        //    //    stream.Flush();

        //    //    for (int i = 0; i < mbCount; i++)
        //    //    {
        //    //        stream.Write(bytesFotMb, 0, bytesFotMb.Length);
        //    //    }
        //    //})).Result;

        //    var cts = new CancellationTokenSource();
        //    AutoResetEvent ev = new AutoResetEvent(false);
        //    var reqContent = new HttpRequestMessage(HttpMethod.Post, url)
        //    {
        //        Content = new WriteToStreamContent((stream, ctx) =>
        //        {
        //            for (int i = 0; i < mbCount; i++)
        //            {
        //                stream.Write(bytesFotMb, 0, bytesFotMb.Length);
        //            }
        //        }),
        //    };
        //    // workaround for net core 2.2
        //    reqContent.Headers.ExpectContinue = true;
        //    var response = httpClient.SendAsync(reqContent, HttpCompletionOption.ResponseContentRead, cts.Token).Result;
        //    if (response.IsSuccessStatusCode)
        //    {
        //        using (var streamReader = new StreamReader(response.Content.ReadAsStreamAsync().Result))
        //        {
        //            Console.WriteLine("response-{0}", streamReader.ReadToEnd());
        //        };
        //    }

        //    stopwatch.Stop();
        //    Console.WriteLine("elapsed-{0}", stopwatch.Elapsed);

        //    Console.ReadKey();
        //}
    }

    //public class WriteToStreamContent : HttpContent
    //{
    //    private readonly Action<Stream, HttpContent> writeAction;
    //    private readonly Encoding encoding;

    //    public WriteToStreamContent(Action<Stream, HttpContent> writeAction)
    //    {
    //        this.writeAction = writeAction;
    //    }

    //    protected override async Task SerializeToStreamAsync(Stream stream,
    //        TransportContext context)
    //    {
    //        writeAction(stream, this);
    //    }

    //    protected override bool TryComputeLength(out long length)
    //    {
    //        length = -1;
    //        return false;
    //    }

    //    protected override Task<Stream> CreateContentReadStreamAsync()
    //    {
    //        return base.CreateContentReadStreamAsync();
    //    }
    //}
}
