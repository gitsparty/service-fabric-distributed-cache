using System;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    class CacheStoreNotFoundException : Exception
    {
        public CacheStoreNotFoundException(string message) : base(message)
        {

        }
    }
}
