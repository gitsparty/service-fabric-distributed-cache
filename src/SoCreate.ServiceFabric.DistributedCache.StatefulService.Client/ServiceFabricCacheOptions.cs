using Microsoft.Extensions.Options;
using System;
using System.Fabric;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public class ServiceFabricCacheOptions : IOptions<ServiceFabricCacheOptions>
    {
        public string CacheStoreServiceUri { get; set; }

        public string CacheStoreEndpointName { get; set; }

        public Guid CacheStoreId { get; set; }

        public bool IsActorService { get; set; }

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
            if (string.IsNullOrWhiteSpace(CacheStoreServiceUri))
            {
                throw new ArgumentException("Config Value has to be set", "StoreConfig.ServiceUri");
            }

            CacheStoreEndpointName = configurationPackage.Settings.Sections["StoreConfig"].Parameters["ServiceEndpointName"].Value;
            if (string.IsNullOrWhiteSpace(CacheStoreEndpointName))
            {
                throw new ArgumentException("Config Value has to be set", "StoreConfig.ServiceEndpointName");
            }

            var val = configurationPackage.Settings.Sections["StoreConfig"].Parameters["IsActorService"].Value;
            IsActorService = (bool)Convert.ChangeType(val, typeof(bool));
        }
    }
}