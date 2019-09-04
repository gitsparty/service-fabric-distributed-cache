using System.Threading;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    //
    // Copy the definition of IDistributedCache so that it can be remoted.
    //
    public interface IDistributedCacheWithCreate : IService
    {
        Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));

        Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));

        Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));

        Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));

        Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token);
    }
}
