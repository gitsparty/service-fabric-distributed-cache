using System.Fabric;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService
{
    internal sealed partial class DistributedCacheStore : DistributedCacheStoreService
    {
        public DistributedCacheStore(StatefulServiceContext context)
            : base(context, (message) => ServiceEventSource.Current.ServiceMessage(context, message))
        { }

        protected override int MaxCacheSizeInMegabytes => 1500;
    }
}
