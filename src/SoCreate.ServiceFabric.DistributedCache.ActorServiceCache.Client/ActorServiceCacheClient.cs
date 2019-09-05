using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using Microsoft.Extensions.Caching.Distributed;
using SoCreate.ServiceFabric.DistributedCache.ActorServiceCache.Interfaces;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;

namespace SoCreate.ServiceFabric.DistributedCache.ActorServiceCache.Client
{
    public class ActorServiceCacheClient : IDistributedCacheWithCreate, IActorServiceCache, IDistributedCache
    {
        private string _serviceUri;

        public ActorServiceCacheClient(string serviceUri)
        {
            _serviceUri = serviceUri;
        }

        public byte[] Get(string key)
        {
            return this.GetAsync(key).Result;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var actor = ActorProxy.Create<IActorServiceCache>(new ActorId(key), new System.Uri(_serviceUri));
            return actor.GetAsync(key, token);
        }

        public void Refresh(string key)
        {
            this.RefreshAsync(key).Wait();
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var actor = ActorProxy.Create<IActorServiceCache>(new ActorId(key), new System.Uri(_serviceUri));
            return actor.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            this.RemoveAsync(key).Wait();
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var actorSvc = ActorServiceProxy.Create(new System.Uri(_serviceUri), new ActorId(key));

            await actorSvc.DeleteActorAsync(new ActorId(key), token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            this.SetAsync(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            var actor = ActorProxy.Create<IActorServiceCache>(new ActorId(key), new System.Uri(_serviceUri));
            return actor.SetAsync(key, value, options, token);
        }

        public Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            var actor = ActorProxy.Create<IActorServiceCache>(new ActorId(key), new System.Uri(_serviceUri));
            return actor.CreateCachedItemAsync(key, value, options, token);
        }
    }
}
