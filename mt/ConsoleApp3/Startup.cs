using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atoll.Transport;
using Atoll.Transport.DataHub;
using Atoll.Transport.ServerBundle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp2
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
            //var mongoDbStore = new MongoDbPacketsStore(new MongoDbParameters("mongodb://localhost:5555", TransportConstants.MongoDatabaseName));
            //MongoDbClusterPacketsStoreUtils.CreateMongoClusterPacketsStore(new DbClusterReservateSettings());
            var mongoUrls = new string[]
            {
                //"mongodb://localhost:5555",
                "mongodb://localhost:3333",
            };

            var clusterParams = new DbClusterServiceParameters(mongoUrls, TransportConstants.MongoDatabaseName);
            var reservationParams = new DbClusterReservationsSettings(TimeSpan.FromSeconds(3));

            services.AddMongoServices(new AssignedUnitIdProvider("dhu2"), new MongoServicesOptions(clusterParams, reservationParams));
            services.AddSingleton<IHostedService, DbClusterServiceHostObjectWrapper>();
            services.AddSingleton<IPacketsStore, DbClusterPacketsStore>(ctx => new DbClusterPacketsStore(ctx.GetRequiredService<IDbClusterService>(),
                dbParameters => new MongoDbPacketsStore(new MongoDbParameters(dbParameters.ConnStringOrUrl, dbParameters.DatabaseName))));
            services.AddSingleton<ITransportRequestService, HttpMessageRequestParser>();

            var agentConfigurationsHandler = new AgentConfigurationsService();
            agentConfigurationsHandler.AddWithKeyOrThrow(new TestAgentDynamicConfigurationProvider());
            var queueManager = new LimitConcurrentQueueManager(50, TimeSpan.FromSeconds(36));

            services.AddSingleton<IThrottleQueueManager>(queueManager);
            services.AddSingleton<IAgentStaticConfigService, AgentStaticConfigService>();
            services.AddSingleton<IDbTokenService, DbTokenService>();
            services.AddSingleton<IAgentConfigurationsService>(agentConfigurationsHandler);

            services.AddMvc()
                //
                .AddApplicationPart(typeof(TransportController).Assembly)
                .AddControllersAsServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseMiddleware<TransportMiddleware>();
            app.UseMvc();
        }
    }
}
