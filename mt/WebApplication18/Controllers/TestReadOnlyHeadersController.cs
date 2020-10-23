using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication18.Controllers
{
    [Route("tests/readonlyheaders")]
    public class TestReadOnlyHeadersController : Controller
    {
        // GET: /<controller>/
        [HttpPost("get")]
        [DisableRequestSizeLimit]
        public IActionResult Get()
        {
            return ReadAndResponse();
        }

        [HttpPost("post")]
        [DisableRequestSizeLimit]
        public IActionResult Post()
        {
            return ReadAndResponse();
        }

        private IActionResult ReadAndResponse()
        {
            string bodyStr = "";
            var headers = string.Join(";", this.Request.Headers.Select(x => $"{x.Key}={x.Value}"));
            if (this.Request.Query.Any(x => x.Key.ToLower() == "readbody"))
            {
                using (StreamReader reader
                  = new StreamReader(this.Request.Body, Encoding.UTF8, true, 1024, true))
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

                return this.Json(new { headers, bodyStr });
            }

            // http status code 429 - The user has sent too many requests in a given amount of time ("rate limiting").
            var retryAfterMs = 36 * 1000;
            this.Response.Headers.Add("Retry-After", retryAfterMs.ToString());
            var reponseData = new { };
            return this.StatusCode(429, reponseData);
        }
    }
}
