using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace SoCreate.Extensions.Caching.ServiceFabric
{
    public interface IDistributedCacheWithCreate : IDistributedCache
    {
        Task<CreateItemResult> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options);
    }
}
