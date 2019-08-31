using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using SoCreate.ServiceFabric.DistributedCache.StatefulService.Client;
using System;
using System.Fabric;

namespace SoCreate.ServiceFabric.DistributedCache.StatelessService
{
    public static class ServiceFabricCachingServicesExtensions
    {
        public static IServiceCollection AddDistributedServiceFabricCache(
            this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddOptions<ServiceFabricCacheOptions>().Configure<StatelessServiceContext>(
                (option, context) => 
                {
                    option.Initialize(context);
                });

            return services
                .AddSingleton<IDistributedCacheStoreLocator, DistributedCacheStoreLocator>()
                .AddSingleton<ISystemClock, SystemClock>()
                .AddSingleton<IDistributedCacheWithCreate, ServiceFabricDistributedCacheClient>();
        }
    }
}
