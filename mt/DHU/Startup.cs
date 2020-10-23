using System;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Atoll.Transport;
using Atoll.Transport.DataHub;
using Atoll.Transport.ServerBundle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Serialization;

namespace DHU
{
    public class DbClusterServiceHostObjectWrapper : IHostedService
    {
        private readonly IDbClusterService dbClusterService;

        public DbClusterServiceHostObjectWrapper(IDbClusterService dbClusterService)
        {
            this.dbClusterService = dbClusterService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return this.dbClusterService.StartSearchTask(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return this.dbClusterService.DisposeAsync(cancellationToken);
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mongoUrls = new string[]
            {
                "mongodb://192.168.100.184:27017",
               // "mongodb://localhost:3333",
            };

            var clusterParams = new DbClusterServiceParameters(mongoUrls, TransportConstants.MongoDatabaseName, TransportConstants.DefaultLeaseCheckTimeout, TransportConstants.DefaultLeaseLostTimeout, TransportConstants.DefaultUpdateServerTimeInterval);
            var reservationParams = new DbClusterReservationsSettings(TimeSpan.FromSeconds(3));

            services.AddMongoServices(new ComputerNameUnitIdProvider(TimeSpan.FromMinutes(30)), new MongoServicesOptions(clusterParams, reservationParams));
            services.AddSingleton<IHostedService, DbClusterServiceHostObjectWrapper>();
            services.AddSingleton<IPacketsStore, DbClusterPacketsStore>(ctx => new DbClusterPacketsStore(ctx.GetRequiredService<IDbClusterService>(),
                dbParameters => new MongoDbPacketsStore(new MongoDbParameters(dbParameters.ConnStringOrUrl, dbParameters.DatabaseName))));
            services.AddSingleton<ITransportRequestService, HttpMessageRequestParser>();

            var agentConfigurationsHandler = new AgentConfigurationsService();
            agentConfigurationsHandler.AddWithKeyOrThrow(new TestAgentDynamicConfigurationProvider());
            var queueManager = new LimitConcurrentQueueManager(3, TimeSpan.FromSeconds(36));

            services.AddSingleton<IThrottleQueueManager>(queueManager);
            services.AddSingleton<IAgentStaticConfigService, AgentStaticConfigService>();
            services.AddSingleton<IDbTokenService, DbTokenService>();
            services.AddSingleton<IAgentConfigurationsService>(agentConfigurationsHandler);

            services.AddMvcCore().AddFormatterMappings()
                .AddJsonFormatters()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver =
                        new CamelCasePropertyNamesContractResolver();
                })
                .AddMvcOptions(options =>
                {
                    var formatter = options.InputFormatters.OfType<JsonInputFormatter>().First();
                    formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(MediaTypeNames.Text.Html));
                    formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue(MediaTypeNames.Application.Octet));
                })
                //
                .AddApplicationPart(typeof(TransportController).Assembly)
                .AddControllersAsServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.UseMiddleware<TransportMiddleware>();
            app.UseMvc();
        }
    }
}
