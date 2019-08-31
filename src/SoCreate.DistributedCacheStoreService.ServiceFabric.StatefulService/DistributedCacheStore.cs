using System.Fabric;

namespace SoCreate.DistributedCacheStoreService.ServiceFabric.StatefulService
{
    internal sealed partial class DistributedCacheStore : DistributedCacheStoreService
    {
        public DistributedCacheStore(StatefulServiceContext context)
            : base(context, (message) => ServiceEventSource.Current.ServiceMessage(context, message))
        { }

        protected override int MaxCacheSizeInMegabytes => 1500;
    }
}
