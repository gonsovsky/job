using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    [System.Web.Http.Route("tests/readonlyheaders")]
    public class TestReadOnlyHeadersController : ApiController
    {
        // GET: /<controller>/
        //[DisableRequestSizeLimit]
        public HttpResponseMessage Get()
        {
            return ReadAndResponse();
        }

        [System.Web.Http.HttpPost]
        [System.Web.Http.Route("post")]
        //[DisableRequestSizeLimit]
        public HttpResponseMessage Post()
        {
            return ReadAndResponse();
        }

        private HttpResponseMessage ReadAndResponse()
        {
            string bodyStr = "";
            var headers = string.Join(";", this.Request.Headers.Select(x => $"{x.Key}={x.Value}"));
            if (this.Request.RequestUri.ToString().Contains("readbody"))
            {
                using (StreamReader reader
                  = new StreamReader(this.Request.Content.ReadAsStreamAsync().Result, Encoding.UTF8, true, 1024, true))
                {
                    int readed = 0;
                    long count = 0;
                    do
                    {
                        readed = reader.Read();
                        count = count + readed;
                    } while (readed >= 0);
                    bodyStr = "bodyLength="+ count;
                }

                return Request.CreateResponse(200);
                //return this.Json(new { headers, bodyStr });
            }

            // http status code 429 - The user has sent too many requests in a given amount of time ("rate limiting").
            var retryAfterMs = 36 * 1000;
            //this.Response.Headers.Add("Retry-After", retryAfterMs.ToString());
            var reponseData = new { };


            // Create the response
            var response = Request.CreateResponse(429);

            // Set headers for paging
            response.Headers.Add("Retry-After", retryAfterMs.ToString());

            return response;
        }
    }
}
