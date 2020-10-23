//using Atoll.Transport.DataHub;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace WebApplication18.Middleware
//{
//    public class TransportMiddlewareOptions
//    {
//        public PathString Path { get; set; }
//    }

//    public class TransportMiddleware
//    {
//        private readonly RequestDelegate next;
//        private readonly ILogger logger;
//        private readonly IPacketsStore packetsStore;
//        private readonly ITransportRequestService packageRequestParser;
//        private readonly IAgentStaticConfigService agentConfigService;
//        private readonly IDbTokenService dbTokenService;
//        private readonly IThrottleQueueManager queueManager;
//        private readonly IAgentConfigurationsService agentConfigurationsHandler;
//        private readonly TransportMiddlewareOptions options;

//        public TransportMiddleware(RequestDelegate next, ILoggerFactory loggerFactory,
//            TransportMiddlewareOptions options,

//            IPacketsStore packetsStore,
//            ITransportRequestService packageRequestParser,
//            IThrottleQueueManager queueManager,
//            IAgentStaticConfigService agentConfigService,
//            IDbTokenService dbTokenService,
//            IAgentConfigurationsService agentConfigurationsHandler
//            )
//        {
//            this.next = next;
//            this.logger = loggerFactory.CreateLogger<TransportMiddleware>();
//            this.options = options;

//            this.packetsStore = packetsStore;
//            this.packageRequestParser = packageRequestParser;
//            this.queueManager = queueManager;
//            this.agentConfigService = agentConfigService;
//            this.dbTokenService = dbTokenService;
//            this.agentConfigurationsHandler = agentConfigurationsHandler;
//        }

//        public async Task InvokeAsync(HttpContext context)
//        {
//            await next.Invoke(context);

//            // If the request path doesn't match, skip
//            if (!context.Request.Path.Equals(options.Path, StringComparison.OrdinalIgnoreCase))
//            {
//                await next.Invoke(context);
//            }

//            //// Request must be POST with Content-Type: application/x-www-form-urlencoded
//            //if (!context.Request.Method.Equals("POST")
//            //   || !context.Request.HasFormContentType)
//            //{
//            //    context.Response.StatusCode = 400;
//            //    await context.Response.WriteAsync("Bad request.");
//            //    return;
//            //}

//            //// Serialize and return the response
//            //context.Response.ContentType = "application/json";
//            //await context.Response.WriteAsync(JsonConvert.SerializeObject(response, _serializerSettings));
//        }
//    }
//}
