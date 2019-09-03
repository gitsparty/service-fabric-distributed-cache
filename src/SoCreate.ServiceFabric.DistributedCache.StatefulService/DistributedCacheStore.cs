using System;
using System.Fabric;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService
{
    internal sealed class DistributedCacheStore : DistributedCacheStoreService
    {
        private int _maxCacheSizeInMegaBytes = 1500;

        public DistributedCacheStore(StatefulServiceContext context)
            : base(context, (message) => ServiceEventSource.Current.ServiceMessage(context, message))
        {
            var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var value = configurationPackage.Settings.Sections["CacheConfig"].Parameters["MaxCacheSizeInMegabytes"].Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                _maxCacheSizeInMegaBytes = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        protected override int MaxCacheSizeInMegabytes
        {
            get
            {
                return _maxCacheSizeInMegaBytes;
            }
        }
    }
}