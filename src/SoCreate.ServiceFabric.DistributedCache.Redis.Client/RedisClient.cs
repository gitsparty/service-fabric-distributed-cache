using System;
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
        private string _connectionString;
        private ConnectionMultiplexer _mux;

        public RedisClient(string connectionString)
        {
            _connectionString = connectionString;
            _mux = ConnectionMultiplexer.ConnectAsync(connectionString).Result;
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return await _mux.GetDatabase().StringGetAsync(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return this.GetAsync(key, token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var mux = await InitializeAsync();

            await mux.GetDatabase().KeyDeleteAsync(key);
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            var mux = await InitializeAsync();

            await mux.GetDatabase().StringSetAsync(key, value, when: When.Exists);
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            var mux = await InitializeAsync();

            var ret = await mux.GetDatabase().StringSetAsync(key, value, when: When.NotExists);

            return ret ? value : null;
        }

        private Task<ConnectionMultiplexer> InitializeAsync()
        {
            if (_mux == null)
            {
                lock (_mux)
                {
                    if (_mux == null)
                    {
                        return ConnectionMultiplexer.ConnectAsync(_connectionString);
                    }
                    else
                    {
                        return Task.FromResult(_mux);
                    }
                }
            }
            else
            {
                return Task.FromResult(_mux);
            }
        }
    }
}
