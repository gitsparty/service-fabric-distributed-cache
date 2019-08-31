using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public interface IServiceFabricCacheStoreService : IService
    {
        Task<byte[]> CreateCachedItemAsync(string key, byte[] value, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration);

        Task<byte[]> GetCachedItemAsync(string key);
        Task SetCachedItemAsync(string key, byte[] value, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration);
        Task RemoveCachedItemAsync(string key);
    }
}
