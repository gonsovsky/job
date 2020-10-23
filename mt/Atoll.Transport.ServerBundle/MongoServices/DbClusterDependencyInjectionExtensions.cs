using Microsoft.Extensions.DependencyInjection;

namespace Atoll.Transport.ServerBundle
{
    public class MongoServicesOptions
    {
        public DbClusterServiceParameters DbClusterParameters { get; private set; }
        public DbClusterReservationsSettings Settings { get; private set; }

        public MongoServicesOptions(DbClusterServiceParameters dbClusterParameters, DbClusterReservationsSettings settings)
        {
            this.DbClusterParameters = dbClusterParameters;
            this.Settings = settings;
        }
    }

    public static class DbClusterDependencyInjectionExtensions
    {
        public static IServiceCollection AddMongoServices<T>(this IServiceCollection services, MongoServicesOptions options)
            where T: class, IUnitIdProvider
        {
            services.AddSingleton<IUnitIdProvider, T>();
            services.AddSingleton<IDbServicesFactory, MongoDbServicesFactory>();
            services.AddSingleton<IDbClusterSearchUtils, DbClusterSearchUtils>();
            services.AddSingleton<IDbClusterService, DbClusterLeaseService>(ctx =>
            {
                return new DbClusterLeaseService(ctx.GetRequiredService<IDbClusterSearchUtils>(), options.DbClusterParameters, options.Settings);
            });
            return services;
        }

        public static IServiceCollection AddMongoServices(this IServiceCollection services, IUnitIdProvider unitIdProvider, MongoServicesOptions options)
        {
            services.AddSingleton<IUnitIdProvider>(unitIdProvider);
            services.AddSingleton<IDbServicesFactory, MongoDbServicesFactory>();
            services.AddSingleton<IDbClusterSearchUtils, DbClusterSearchUtils>();
            services.AddSingleton<IDbClusterService, DbClusterLeaseService>(ctx =>
            {
                return new DbClusterLeaseService(ctx.GetRequiredService<IDbClusterSearchUtils>(), options.DbClusterParameters, options.Settings);
            });
            return services;
        }
    }

}
