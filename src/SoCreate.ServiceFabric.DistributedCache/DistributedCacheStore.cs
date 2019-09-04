using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public class DistributedCacheStore :
        IServiceFabricDistributedCacheService,
        IServiceFabricDistributedCacheStoreBackgroundWorker
    {
        const int ByteSizeOffset = 250;
        const int DefaultCacheSizeInMegabytes = 100;
        const string CacheStoreName = "CacheStore";
        const string CacheStoreMetadataName = "CacheStoreMetadata";
        const string CacheStoreMetadataKey = "CacheStoreMetadata";
        private readonly Action<string> _log;
        private readonly ISystemClock _systemClock;
        IReliableStateManager _stateManager;
        private readonly int _maxCacheSizeInBytes;

        public DistributedCacheStore(
            IReliableStateManager stateManager,
            ISystemClock systemClock,
            Action<string> log)
        {
            _stateManager = stateManager;
            _log = log;
            _systemClock = systemClock;
        }

        public DistributedCacheStore(
            IReliableStateManager stateManager,
            int maxCacheSizeInBytes,
            Action<string> log = null)
        {
            if (maxCacheSizeInBytes == 0)
            {
                throw new ArgumentException($"Argument {nameof(maxCacheSizeInBytes)} can't be 0");
            }

            _log = log;
            _systemClock = new SystemClock();
            _stateManager = stateManager;
            _maxCacheSizeInBytes = maxCacheSizeInBytes;

            if (!_stateManager.TryAddStateSerializer(new CachedItemSerializer()))
            {
                throw new InvalidOperationException("Failed to set CachedItem custom serializer");
            }

            if (!_stateManager.TryAddStateSerializer(new CacheStoreMetadataSerializer()))
            {
                throw new InvalidOperationException("Failed to set CacheStoreMetadata custom serializer");
            }

            if (!_stateManager.TryAddStateSerializer(new CreateItemResultSerializer()))
            {
                throw new InvalidOperationException("Failed to set CreateItemResultSerializer custom serializer");
            }
        }

        async Task<byte[]> IDistributedCacheWithCreate.CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            (var absoluteExpiration, var slidingExpiration) = SharedWithClients.ValidateAndGetAbsoluteAndSlidingExpirations(
                _systemClock.UtcNow,
                options);

            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);
            var cacheStoreMetadata = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheStoreMetadata>>(CacheStoreMetadataName);

            return await RetryHelper.ExecuteWithRetry(stateManager: _stateManager, cancellationToken: token, operation: async (tx, cancellationToken, state) =>
            {
                _log?.Invoke($"Set cached item called with key: {key}");

                Func<string, Task<ConditionalValue<CachedItem>>> getCacheItem = async (string cacheKey) => await cacheStore.TryGetValueAsync(tx, cacheKey, LockMode.Update);
                var linkedDictionaryHelper = new LinkedDictionaryHelper(getCacheItem, ByteSizeOffset);

                var cacheStoreInfo = (await cacheStoreMetadata.TryGetValueAsync(tx, CacheStoreMetadataKey, LockMode.Update)).Value ?? new CacheStoreMetadata(0, null, null);
                var existingCacheItem = (await getCacheItem(key)).Value;
                var cachedItem = ApplyAbsoluteExpiration(existingCacheItem, absoluteExpiration) ?? new CachedItem(value, null, null, slidingExpiration, absoluteExpiration);

                // empty linked dictionary
                if (cacheStoreInfo.FirstCacheKey == null)
                {
                    var metadata = new CacheStoreMetadata(value.Length + ByteSizeOffset, key, key);
                    await cacheStoreMetadata.SetAsync(tx, CacheStoreMetadataKey, metadata);
                    await cacheStore.SetAsync(tx, key, cachedItem);

                    return cachedItem.Value;
                }
                else
                {
                    var cacheMetadata = cacheStoreInfo;

                    // linked node already exists in dictionary
                    if (existingCacheItem != null)
                    {
                        return null;
                    }

                    // add to last
                    var addLastResult = await linkedDictionaryHelper.AddLast(cacheMetadata, key, cachedItem, value);
                    await ApplyChanges(tx, cacheStore, cacheStoreMetadata, addLastResult);

                    return cachedItem.Value;
                }
            });
        }

        async Task<byte[]> IDistributedCacheWithCreate.GetAsync(string key, CancellationToken token)
        {
            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);

            var cacheResult = await RetryHelper.ExecuteWithRetry(
                stateManager: _stateManager,
                operation: async (tx, cancellationToken, state) =>
                {
                    _log?.Invoke($"Get cached item called with key: {key}");
                    return await cacheStore.TryGetValueAsync(tx, key);
                },
                cancellationToken: token);

            if (cacheResult.HasValue)
            {
                var cachedItem = cacheResult.Value;

                // cache item not expired
                if (_systemClock.UtcNow < cachedItem.AbsoluteExpiration)
                {
                    var option = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = cachedItem.AbsoluteExpiration,
                        SlidingExpiration = cachedItem.SlidingExpiration
                    };

                    await ((IDistributedCacheWithCreate)this).SetAsync(key, cachedItem.Value, option, token);
                    return cachedItem.Value;
                }
            }

            return null;
        }

        async Task IDistributedCacheWithCreate.SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            (var absoluteExpiration, var slidingExpiration) = SharedWithClients.ValidateAndGetAbsoluteAndSlidingExpirations(
                _systemClock.UtcNow,
                options);

            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);
            var cacheStoreMetadata = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheStoreMetadata>>(CacheStoreMetadataName);

            await RetryHelper.ExecuteWithRetry(stateManager: _stateManager, cancellationToken: token, operation: async (tx, cancellationToken, state) =>
            {
                _log?.Invoke($"Set cached item called with key: {key}");

                Func<string, Task<ConditionalValue<CachedItem>>> getCacheItem = async (string cacheKey) => await cacheStore.TryGetValueAsync(tx, cacheKey, LockMode.Update);
                var linkedDictionaryHelper = new LinkedDictionaryHelper(getCacheItem, ByteSizeOffset);

                var cacheStoreInfo = (await cacheStoreMetadata.TryGetValueAsync(tx, CacheStoreMetadataKey, LockMode.Update)).Value ?? new CacheStoreMetadata(0, null, null);
                var existingCacheItem = (await getCacheItem(key)).Value;
                var cachedItem = ApplyAbsoluteExpiration(existingCacheItem, absoluteExpiration) ?? new CachedItem(value, null, null, slidingExpiration, absoluteExpiration);

                // empty linked dictionary
                if (cacheStoreInfo.FirstCacheKey == null)
                {
                    var metadata = new CacheStoreMetadata(value.Length + ByteSizeOffset, key, key);
                    await cacheStoreMetadata.SetAsync(tx, CacheStoreMetadataKey, metadata);
                    await cacheStore.SetAsync(tx, key, cachedItem);
                }
                else
                {
                    var cacheMetadata = cacheStoreInfo;

                    // linked node already exists in dictionary
                    if (existingCacheItem != null)
                    {
                        var removeResult = await linkedDictionaryHelper.Remove(cacheStoreInfo, cachedItem);
                        cacheMetadata = removeResult.CacheStoreMetadata;
                        await ApplyChanges(tx, cacheStore, cacheStoreMetadata, removeResult);
                    }

                    // add to last
                    var addLastResult = await linkedDictionaryHelper.AddLast(cacheMetadata, key, cachedItem, value);
                    await ApplyChanges(tx, cacheStore, cacheStoreMetadata, addLastResult);
                }
            });
        }

        async Task IDistributedCacheWithCreate.RemoveAsync(string key, CancellationToken token)
        {
            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);
            var cacheStoreMetadata = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheStoreMetadata>>(CacheStoreMetadataName);

            await RetryHelper.ExecuteWithRetry(_stateManager, async (tx, cancellationToken, state) =>
            {
                _log?.Invoke($"Remove cached item called with key: {key}");

                var cacheResult = await cacheStore.TryRemoveAsync(tx, key);
                if (cacheResult.HasValue)
                {
                    Func<string, Task<ConditionalValue<CachedItem>>> getCacheItem = async (string cacheKey) => await cacheStore.TryGetValueAsync(tx, cacheKey, LockMode.Update);
                    var linkedDictionaryHelper = new LinkedDictionaryHelper(getCacheItem, ByteSizeOffset);

                    var cacheStoreInfo = (await cacheStoreMetadata.TryGetValueAsync(tx, CacheStoreMetadataKey, LockMode.Update)).Value ?? new CacheStoreMetadata(0, null, null);
                    var result = await linkedDictionaryHelper.Remove(cacheStoreInfo, cacheResult.Value);

                    await ApplyChanges(tx, cacheStore, cacheStoreMetadata, result);
                }
            });
        }

        Task IDistributedCacheWithCreate.RefreshAsync(string key, CancellationToken token)
        {
            return ((IDistributedCacheWithCreate)this).GetAsync(key, token);
        }

        async Task IServiceFabricDistributedCacheStoreBackgroundWorker.RunAsync(CancellationToken cancellationToken)
        {
            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);
            var cacheStoreMetadata = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheStoreMetadata>>(CacheStoreMetadataName);

            while (true)
            {
                await RemoveLeastRecentlyUsedCacheItemWhenOverMaxCacheSize(cancellationToken);
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
            }
        }

        protected async Task RemoveLeastRecentlyUsedCacheItemWhenOverMaxCacheSize(CancellationToken cancellationToken)
        {
            var cacheStore = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CachedItem>>(CacheStoreName);
            var cacheStoreMetadata = await _stateManager.GetOrAddAsync<IReliableDictionary<string, CacheStoreMetadata>>(CacheStoreMetadataName);
            bool continueRemovingItems = true;

            while (continueRemovingItems)
            {
                continueRemovingItems = false;
                cancellationToken.ThrowIfCancellationRequested();

                await RetryHelper.ExecuteWithRetry(_stateManager, async (tx, cancelToken, state) =>
                {
                    var metadata = await cacheStoreMetadata.TryGetValueAsync(tx, CacheStoreMetadataKey, LockMode.Update);

                    if (metadata.HasValue)
                    {
                        _log?.Invoke($"Size: {metadata.Value.Size}  Max Size: {GetMaxSizeInBytes()}");

                        if (metadata.Value.Size > GetMaxSizeInBytes())
                        {
                            Func<string, Task<ConditionalValue<CachedItem>>> getCacheItem = async (string cacheKey) => await cacheStore.TryGetValueAsync(tx, cacheKey, LockMode.Update);
                            var linkedDictionaryHelper = new LinkedDictionaryHelper(getCacheItem, ByteSizeOffset);

                            var firstItemKey = metadata.Value.FirstCacheKey;
                            var firstCachedItem = (await getCacheItem(firstItemKey)).Value;

                            // Move item to last item if cached item is not expired
                            if (firstCachedItem.AbsoluteExpiration > _systemClock.UtcNow)
                            {
                                // remove cached item
                                var removeResult = await linkedDictionaryHelper.Remove(metadata.Value, firstCachedItem);
                                await ApplyChanges(tx, cacheStore, cacheStoreMetadata, removeResult);

                                // add to last
                                var addLastResult = await linkedDictionaryHelper.AddLast(removeResult.CacheStoreMetadata, firstItemKey, firstCachedItem, firstCachedItem.Value);
                                await ApplyChanges(tx, cacheStore, cacheStoreMetadata, addLastResult);

                                continueRemovingItems = addLastResult.CacheStoreMetadata.Size > GetMaxSizeInBytes();
                            }
                            else  // Remove 
                            {
                                _log?.Invoke($"Auto Removing: {metadata.Value.FirstCacheKey}");

                                var result = await linkedDictionaryHelper.Remove(metadata.Value, firstCachedItem);
                                await ApplyChanges(tx, cacheStore, cacheStoreMetadata, result);
                                await cacheStore.TryRemoveAsync(tx, metadata.Value.FirstCacheKey);

                                continueRemovingItems = result.CacheStoreMetadata.Size > GetMaxSizeInBytes();
                            }
                        }
                    }
                });
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        private int GetMaxSizeInBytes()
        {
            return _maxCacheSizeInBytes;
        }

        private async Task ApplyChanges(ITransaction tx, IReliableDictionary<string, CachedItem> cachedItemStore, IReliableDictionary<string, CacheStoreMetadata> cacheStoreMetadata, LinkedDictionaryItemsChanged linkedDictionaryItemsChanged)
        {
            foreach (var cacheItem in linkedDictionaryItemsChanged.CachedItemsToUpdate)
            {
                await cachedItemStore.SetAsync(tx, cacheItem.Key, cacheItem.Value);
            }
    
            await cacheStoreMetadata.SetAsync(tx, CacheStoreMetadataKey, linkedDictionaryItemsChanged.CacheStoreMetadata);
        }

        private CachedItem ApplyAbsoluteExpiration(CachedItem cachedItem, DateTimeOffset? absoluteExpiration)
        {
            if (cachedItem != null)
            {
                return new CachedItem(cachedItem.Value, cachedItem.BeforeCacheKey, cachedItem.AfterCacheKey, cachedItem.SlidingExpiration, absoluteExpiration);
            }
            return null;
        }
    }
}
