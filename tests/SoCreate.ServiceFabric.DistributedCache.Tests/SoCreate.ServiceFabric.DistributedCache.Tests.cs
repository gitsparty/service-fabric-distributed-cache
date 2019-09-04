using AutoFixture.Xunit2;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Moq;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SoCreate.ServiceFabric.DistributedCache.Tests
{
    public class DistributedCacheStoreServiceTest
    {
        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatExistsWithSlidingExpiration_ItemIsMovedToLastItem(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var options = new DistributedCacheEntryOptions();
            options.SetSlidingExpiration(TimeSpan.FromSeconds(10));
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, cacheItemDict);
            var metadata = SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("mykey1", cacheValue, options, default(CancellationToken));
            await cacheStore.SetAsync("mykey2", cacheValue, options, default(CancellationToken));
            await cacheStore.SetAsync("mykey3", cacheValue, options, default(CancellationToken));

            Assert.Equal("mykey3", metadata["CacheStoreMetadata"].LastCacheKey);

            await cacheStore.GetAsync("mykey2", default(CancellationToken));

            Assert.Equal("mykey2", metadata["CacheStoreMetadata"].LastCacheKey);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatExistsWithAbsoluteExpiration_ItemIsMovedToLastItem(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            var expireTime = currentTime.AddSeconds(30);
            options.SetAbsoluteExpiration(expireTime);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, cacheItemDict);
            var metadata = SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("mykey1", cacheValue, options, default(CancellationToken));
            await cacheStore.SetAsync("mykey2", cacheValue, options, default(CancellationToken));
            await cacheStore.SetAsync("mykey3", cacheValue, options, default(CancellationToken));

            Assert.Equal("mykey3", metadata["CacheStoreMetadata"].LastCacheKey);

            await cacheStore.GetAsync("mykey2", default(CancellationToken));

            Assert.Equal("mykey2", metadata["CacheStoreMetadata"].LastCacheKey);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesNotExist_NullResultReturned(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            var expireTime = currentTime.AddSeconds(1);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            var result = await cacheStore.GetAsync("keyThatDoesNotExist");
            Assert.Null(result);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsNotAbsoluteExpired_CachedItemReturned(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            var expireTime = currentTime.AddSeconds(1);
            options.SetAbsoluteExpiration(expireTime);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);
            var result = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, result);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsAbsoluteExpired_NullResultReturned(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetAbsoluteExpiration(currentTime.AddSeconds(1));

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(2));
            var result = await cacheStore.GetAsync("mykey");
            Assert.Null(result);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsAbsoluteExpiredDoesNotSlideTime_ExpireTimeDoesNotSlide(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            var expireTime = currentTime.AddSeconds(5);
            options.SetAbsoluteExpiration(expireTime);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);
            var result = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, result);

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(5));

            var resultAfter6Seconds = await cacheStore.GetAsync("mykey");
            Assert.Null(resultAfter6Seconds);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsNotSlidingExpired_CachedItemReturned(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(1));

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);
            var result = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, result);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsSlidingExpired_NullResultReturned(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(1));

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, metadataDict);
            SetupInMemoryStores(stateManager, cacheItemDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(2));
            var result = await cacheStore.GetAsync("mykey");
            Assert.Null(result);
        }

        [Theory, AutoMoqData]
        async void GetCachedItemAsync_GetItemThatDoesHaveKeyAndIsIsSlidingExpired_SlidedExpirationUpdates(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(10));
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            SetupInMemoryStores(stateManager, cacheItemDict);
            SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("mykey", cacheValue, options);
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(5));
            var resultAfter5Seconds = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, resultAfter5Seconds);
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(8));
            var resultAfter8Seconds = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, resultAfter8Seconds);
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(9));
            var resultAfter9Seconds = await cacheStore.GetAsync("mykey");
            Assert.Equal(cacheValue, resultAfter9Seconds);
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(19));
            var resultAfter19Seconds = await cacheStore.GetAsync("mykey");
            Assert.Null(resultAfter19Seconds);
        }

        [Theory, AutoMoqData]
        async void SetCachedItemAsync_AddItemsToCreateLinkedDictionary_DictionaryCreatedWithItemsLinked(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(10));

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            var cachedItems = SetupInMemoryStores(stateManager, cacheItemDict);
            var metadata = SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("1", cacheValue, options);
            await cacheStore.SetAsync("2", cacheValue, options);
            await cacheStore.SetAsync("3", cacheValue, options);
            await cacheStore.SetAsync("4", cacheValue, options);

            Assert.Null(cachedItems["1"].BeforeCacheKey);
            foreach (var item in cachedItems)
            {
                if (item.Value.BeforeCacheKey != null)
                {
                    Assert.Equal(item.Key, cachedItems[item.Value.BeforeCacheKey].AfterCacheKey);
                }
                if (item.Value.AfterCacheKey != null)
                {
                    Assert.Equal(item.Key, cachedItems[item.Value.AfterCacheKey].BeforeCacheKey);
                }
            }
            Assert.Null(cachedItems["4"].AfterCacheKey);

            Assert.Equal("1", metadata["CacheStoreMetadata"].FirstCacheKey);
            Assert.Equal("4", metadata["CacheStoreMetadata"].LastCacheKey);
            Assert.Equal((cacheValue.Length + 250) * cachedItems.Count, metadata["CacheStoreMetadata"].Size);
        }

        [Theory, AutoMoqData]
        async void RemoveCachedItemAsync_RemoveItemsFromLinkedDictionary_ListStaysLinkedTogetherAfterItemsRemoved(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = Encoding.UTF8.GetBytes("someValue");
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(10));

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            var cachedItems = SetupInMemoryStores(stateManager, cacheItemDict);
            var metadata = SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("1", cacheValue, options);
            await cacheStore.SetAsync("2", cacheValue, options);
            await cacheStore.SetAsync("3", cacheValue, options);
            await cacheStore.SetAsync("4", cacheValue, options);
            await cacheStore.SetAsync("5", cacheValue, options);
            await cacheStore.SetAsync("6", cacheValue, options);
            await cacheStore.SetAsync("7", cacheValue, options);
            await cacheStore.SetAsync("8", cacheValue, options);

            await cacheStore.RemoveAsync("3");
            await cacheStore.RemoveAsync("4");
            await cacheStore.RemoveAsync("8");
            await cacheStore.RemoveAsync("1");

            Assert.Null(cachedItems["2"].BeforeCacheKey);
            foreach (var item in cachedItems)
            {
                if (item.Value.BeforeCacheKey != null)
                {
                    Assert.Equal(item.Key, cachedItems[item.Value.BeforeCacheKey].AfterCacheKey);
                }
                if (item.Value.AfterCacheKey != null)
                {
                    Assert.Equal(item.Key, cachedItems[item.Value.AfterCacheKey].BeforeCacheKey);
                }
            }
            Assert.Null(cachedItems["7"].AfterCacheKey);

            Assert.Equal("2", metadata["CacheStoreMetadata"].FirstCacheKey);
            Assert.Equal("7", metadata["CacheStoreMetadata"].LastCacheKey);
            Assert.Equal((cacheValue.Length + 250) * cachedItems.Count, metadata["CacheStoreMetadata"].Size);
        }

        [Theory, AutoMoqData]
        async void RemoveLeastRecentlyUsedCacheItemWhenOverMaxCacheSize_RemoveItemsFromLinkedDictionary_DoesNotRemoveNonExpiredItems(
            [Frozen]Mock<IReliableStateManager> stateManager,
            [Frozen]Mock<IReliableDictionary<string, CachedItem>> cacheItemDict,
            [Frozen]Mock<IReliableDictionary<string, CacheStoreMetadata>> metadataDict,
            [Frozen]Mock<ISystemClock> systemClock,
            [Greedy]DistributedCacheStoreService cacheStoreObject)
        {
            var cacheStore = (IServiceFabricCacheStoreService)cacheStoreObject;
            var backgroundWorker = (IServiceFabricCacheStoreBackgroundWorker)cacheStoreObject;
            var options = new DistributedCacheEntryOptions();
            var cacheValue = new byte[1000000];
            var currentTime = new DateTime(2019, 2, 1, 1, 0, 0);
            options.SetSlidingExpiration(TimeSpan.FromMinutes(10));
            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime);

            var cachedItems = SetupInMemoryStores(stateManager, cacheItemDict);
            var metadata = SetupInMemoryStores(stateManager, metadataDict);

            await cacheStore.SetAsync("1", cacheValue, options);
            options.SetSlidingExpiration(TimeSpan.FromSeconds(10));
            for (var i = 2; i <= 10; i++)
            {
                await cacheStore.SetAsync(i.ToString(), cacheValue, options);
            }

            systemClock.SetupGet(m => m.UtcNow).Returns(currentTime.AddSeconds(10));

            var src = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            try
            {
                await backgroundWorker.RunAsync(src.Token);
            }
            catch (OperationCanceledException ex)
            {

            }

            Assert.Single(cachedItems);
            Assert.Equal("1", metadata["CacheStoreMetadata"].FirstCacheKey);
            Assert.Equal("1", metadata["CacheStoreMetadata"].LastCacheKey);
        }

        private Dictionary<TKey, TValue> SetupInMemoryStores<TKey, TValue>(
            Mock<IReliableStateManager> stateManager,
            Mock<IReliableDictionary<TKey, TValue>> reliableDict) 
            where TKey : IComparable<TKey>, IEquatable<TKey>
        {
            var inMemoryDict = new Dictionary<TKey, TValue>();
            Func<TKey, ConditionalValue<TValue>> getItem = (key) => inMemoryDict.ContainsKey(key) ? new ConditionalValue<TValue>(true, inMemoryDict[key]) : new ConditionalValue<TValue>(false, default(TValue));

            stateManager.Setup(m => m.GetOrAddAsync<IReliableDictionary<TKey, TValue>>(It.IsAny<string>())).Returns(Task.FromResult(reliableDict.Object));
            reliableDict.Setup(m => m.TryGetValueAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>())).Returns((ITransaction t, TKey key) => Task.FromResult(getItem(key)));
            reliableDict.Setup(m => m.TryGetValueAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>(), It.IsAny<LockMode>())).Returns((ITransaction t, TKey key, LockMode l) => Task.FromResult(getItem(key)));
            reliableDict.Setup(m => m.SetAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>(), It.IsAny<TValue>())).Returns((ITransaction t, TKey key, TValue ci) => { inMemoryDict[key] = ci; return Task.CompletedTask; });
            reliableDict.Setup(m => m.TryRemoveAsync(It.IsAny<ITransaction>(), It.IsAny<TKey>())).Returns((ITransaction t, TKey key) => { var r = getItem(key); inMemoryDict.Remove(key); return Task.FromResult(r); });

            return inMemoryDict;
        }
    }
}
