﻿using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication18.Transport
{


    public interface ITransportRequestService
    {

        MessageHeaders GetHeaders(HttpRequest request);

        Task<ParseBodyAndSavePacketsResult> ParseBodyAndSavePackets(MessageHeaders messageHeaders, HttpRequest request);

    }
    
}
