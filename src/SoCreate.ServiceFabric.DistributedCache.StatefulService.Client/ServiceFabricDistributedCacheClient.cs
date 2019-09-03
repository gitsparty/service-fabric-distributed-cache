using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public class ServiceFabricDistributedCacheClient : IDistributedCacheWithCreate
    {
        private readonly IDistributedCacheStoreLocator _distributedCacheStoreLocator;
        private readonly ISystemClock _systemClock;
        private readonly Guid _cacheStoreId;

        public ServiceFabricDistributedCacheClient(
            IOptions<ServiceFabricCacheOptions> options,
            IDistributedCacheStoreLocator distributedCacheStoreLocator,
            ISystemClock systemClock)
        {
            _cacheStoreId = options.Value.CacheStoreId;
            _distributedCacheStoreLocator = distributedCacheStoreLocator;
            _systemClock = systemClock;
        }

        public byte[] Get(string key)
        {
            return GetAsync(key).Result;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            key = FormatCacheKey(key);
            var proxy = await _distributedCacheStoreLocator.GetCacheStoreProxy(key);
            return await proxy.GetAsync(key, token);
        }

        public void Refresh(string key)
        {
            RefreshAsync(key).Wait();
        }

        public async Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            await GetAsync(key, token);
        }

        public void Remove(string key)
        {
            RemoveAsync(key).Wait();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            key = FormatCacheKey(key);
            var proxy = await _distributedCacheStoreLocator.GetCacheStoreProxy(key);
            await proxy.RemoveAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).Wait();
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            SharedWithClients.ValidateAndGetAbsoluteAndSlidingExpirations(_systemClock.UtcNow, options);

            key = FormatCacheKey(key);
            var proxy = await _distributedCacheStoreLocator.GetCacheStoreProxy(key);
            await proxy.SetAsync(key, value, options, token);
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            SharedWithClients.ValidateAndGetAbsoluteAndSlidingExpirations(_systemClock.UtcNow, options);

            key = FormatCacheKey(key);

            var proxy = await _distributedCacheStoreLocator.GetCacheStoreProxy(key);

            return await proxy.CreateCachedItemAsync(key, value, options, token);
        }

        private string FormatCacheKey(string key)
        {
            return $"{_cacheStoreId}-{key}";
        }
    }
}
