using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;

namespace SoCreate.ServiceFabric.DistributedCache.ReliableCollections
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class ReliableCollections : StatefulService, IServiceFabricDistributedCacheService
    {
        private const string cacheName = "SoCreate.ServiceFabric.DistributedCache.ReliableCollections.ReliableCollections";
        private readonly string _listenerName;
        private readonly Uri _serviceUri;

        public ReliableCollections(StatefulServiceContext context)
            : base(context)
        {
            _serviceUri = context.ServiceName;

            var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            _listenerName = configurationPackage.Settings.Sections["StoreConfig"].Parameters["StoreEndpointName"].Value;

            if (string.IsNullOrWhiteSpace(_listenerName))
            {
                throw new ArgumentException("Config Value has to be set", "StoreConfig.StoreEndpointName");
            }
        }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            yield return new ServiceReplicaListener(context =>
                new FabricTransportServiceRemotingListener(context, this), _listenerName);
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);
            var cache2 = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>("Test");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var val = await cache.TryGetValueAsync(tx, key, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
                var val2 = await cache2.TryGetValueAsync(tx, key, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
                return val.Value;
            }
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return this.GetAsync(key, token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await cache.TryRemoveAsync(tx, key, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
            }
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default(CancellationToken))
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);

            using (var tx = this.StateManager.CreateTransaction())
            {
                await cache.SetAsync(tx, key, value, timeout: TimeSpan.FromSeconds(4), cancellationToken: token);
            }
        }

        public async Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            var cache = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>(cacheName);
            var cache2 = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, bool>>("Test");

            using (var tx = this.StateManager.CreateTransaction())
            {
                try
                {
                    var added2 = await cache2.TryAddAsync(
                        tx,
                        key,
                        true,
                        timeout: TimeSpan.FromSeconds(4),
                        cancellationToken: token);

                    var added = await cache.TryAddAsync(
                        tx,
                        key,
                        value,
                        timeout: TimeSpan.FromSeconds(4),
                        cancellationToken: token);

                    if (added)
                    {
                        await tx.CommitAsync();
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }
    }
}
