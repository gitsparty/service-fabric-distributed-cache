using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public interface IDistributedCacheWithCreate : IDistributedCache
    {
        Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options);
    }
}
