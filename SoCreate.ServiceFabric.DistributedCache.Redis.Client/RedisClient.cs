using System.Threading;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using ServiceStack.Redis;

namespace SoCreate.ServiceFabric.DistributedCache.Redis.Client
{
    public class RedisClient : IDistributedCacheWithCreate
    {
        private ConnectionMultiplexer _mux;
        
        public RedisClient(string connectionString)
        {
            var _mux = ConnectionMultiplexer.ConnectAsync(connectionString);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return System.Convert.FromBase64String(await _mux.GetDatabase().StringGetAsync(key));
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return this.GetAsync(key, token);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _mux.GetDatabase().KeyDeleteAsync(key);
        }

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            return _mux.GetDatabase().StringSetAsync(key, value, when: When.Exists);
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            var ret = await _mux.GetDatabase().StringSetAsync(key, value, when: When.NotExists);

            return ret ? value : null;
        }
    }
}
