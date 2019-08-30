using System;

namespace SoCreate.DistributedCacheStoreService.ServiceFabric.Client
{
    class CacheStoreNotFoundException : Exception
    {
        public CacheStoreNotFoundException(string message) : base(message)
        {

        }
    }
}
