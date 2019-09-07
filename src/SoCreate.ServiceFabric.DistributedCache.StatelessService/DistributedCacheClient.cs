using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;
using SoCreate.ServiceFabric.DistributedCache.StatefulService.Client;
using SoCreate.ServiceFabric.DistributedCache.ActorServiceCache.Client;
using SoCreate.ServiceFabric.DistributedCache.Redis.Client;

namespace SoCreate.ServiceFabric.DistributedCache.StatelessService
{
    public class DistributedCacheClient : IDistributedCacheWithCreate
    {
        private IDistributedCacheWithCreate _resolvedClient;

        public DistributedCacheClient(
            IOptions<ServiceFabricCacheOptions> options,
            IDistributedCacheStoreLocator distributedCacheStoreLocator,
            ISystemClock systemClock)
        {
            if (options.Value.IsActorService)
            {
                _resolvedClient = new ActorServiceCacheClient(options.Value.CacheStoreServiceUri);
            }
            else if (!string.IsNullOrWhiteSpace(options.Value.RedisConectionString))
            {
                _resolvedClient = new RedisClient(options.Value.RedisConectionString);
            }
            else
            {
                _resolvedClient = new ServiceFabricDistributedCacheClient(
                    options,
                    distributedCacheStoreLocator,
                    systemClock);
            }
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _resolvedClient.GetAsync(key, token);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _resolvedClient.RefreshAsync(key, token);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _resolvedClient.RemoveAsync(key, token);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            return _resolvedClient.SetAsync(key, value, options, token);
        }

        public Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            return _resolvedClient.CreateCachedItemAsync(key, value, options, token);
        }

    }
}
