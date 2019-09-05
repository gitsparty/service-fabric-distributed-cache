using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors.Client;
using SoCreate.ServiceFabric.DistributedCache.ActorServiceCache.Interfaces;

namespace SoCreate.ServiceFabric.DistributedCache.ActorServiceCache
{
    /// <remarks>
    /// This class represents an actor.
    /// Every ActorID maps to an instance of this class.
    /// The StatePersistence attribute determines persistence and replication of actor state:
    ///  - Persisted: State is written to disk and replicated.
    ///  - Volatile: State is kept in memory only and replicated.
    ///  - None: State is kept in memory only and not replicated.
    /// </remarks>
    [StatePersistence(StatePersistence.Volatile)]
    internal class ActorServiceCache : Actor, IActorServiceCache
    {
        private const string ValueKey = "valuekey";

        /// <summary>
        /// Initializes a new instance of ActorServiceCache
        /// </summary>
        /// <param name="actorService">The Microsoft.ServiceFabric.Actors.Runtime.ActorService that will host this actor instance.</param>
        /// <param name="actorId">The Microsoft.ServiceFabric.Actors.ActorId for this actor instance.</param>
        public ActorServiceCache(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// An actor is activated the first time any of its methods are invoked.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Actor activated.");

            // The StateManager is this actor's private state store.
            // Data stored in the StateManager will be replicated for high-availability for actors that use volatile or persisted state storage.
            // Any serializable object can be saved in the StateManager.
            // For more information, see https://aka.ms/servicefabricactorsstateserialization

            return this.StateManager.TryAddStateAsync<byte[]>(ValueKey, null);
        }

        public async Task<byte[]> GetAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            var v = await this.StateManager.TryGetStateAsync<byte[]>(ValueKey, token);

            return v.Value;
        }

        public Task RefreshAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            return this.GetAsync(key, token);
        }

        public async Task RemoveAsync(
            string key,
            CancellationToken token = default(CancellationToken))
        {
            await this.StateManager.TryRemoveStateAsync(ValueKey, token);
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            await this.StateManager.SetStateAsync(ValueKey, value, token);
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            await this.StateManager.AddStateAsync(ValueKey, value, token);

            return value;
        }
    }
}
