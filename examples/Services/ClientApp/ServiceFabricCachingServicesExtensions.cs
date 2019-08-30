using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using SoCreate.DistributedCacheStoreService.ServiceFabric.Client;
using System;

namespace ClientApp
{
    public static class ServiceFabricCachingServicesExtensions
    {
        public static IServiceCollection AddDistributedServiceFabricCache(this IServiceCollection services, Action<ServiceFabricCacheOptions> setupAction = null)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (setupAction == null) {
                setupAction = (s) => { };
            }

            services.AddOptions();
            services.Configure(setupAction);

            return services
                .AddSingleton<IDistributedCacheStoreLocator, DistributedCacheStoreLocator>()
                .AddSingleton<ISystemClock, SystemClock>()
                .AddSingleton<IDistributedCacheWithCreate, ServiceFabricDistributedCache>();
        }
    }
}
