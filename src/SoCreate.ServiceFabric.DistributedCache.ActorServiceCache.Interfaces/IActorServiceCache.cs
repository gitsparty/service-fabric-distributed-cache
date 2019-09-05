using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Actors;

namespace SoCreate.ServiceFabric.DistributedCache.ActorServiceCache.Interfaces
{
    /// <summary>
    /// This interface defines the methods exposed by an actor.
    /// Clients use this interface to interact with the actor that implements it.
    /// </summary>
    public interface IActorServiceCache : IActor
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
