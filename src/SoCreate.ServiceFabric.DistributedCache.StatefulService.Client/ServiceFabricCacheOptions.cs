using Microsoft.Extensions.Options;
using System;
using System.Fabric;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public class ServiceFabricCacheOptions : IOptions<ServiceFabricCacheOptions>
    {
        public ServiceFabricCacheOptions Value => this;

        public ServiceFabricCacheOptions()
        {
            this.CacheStoreServiceUri = "*";
            this.CacheStoreEndpointName = "CacheStoreServiceListener";
        }

        public ServiceFabricCacheOptions(ServiceFabricCacheOptions input)
        {
            this.CacheStoreEndpointName = input.CacheStoreEndpointName;
            this.CacheStoreId = input.CacheStoreId;
            this.CacheStoreServiceUri = input.CacheStoreServiceUri;
        }

        public void Initialize(StatelessServiceContext context)
        {
            var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            CacheStoreServiceUri = configurationPackage.Settings.Sections["StoreConfig"].Parameters["ServiceUri"].Value;
        }

        public string CacheStoreServiceUri { get; set; }

        public string CacheStoreEndpointName { get; set; }

        public Guid CacheStoreId { get; set; }
    }
}