using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Rt = Microsoft.ServiceFabric.Services.Runtime;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService
{
    internal sealed class DistributedCacheStatefulService : Rt.StatefulService, IServiceFabricDistributedCacheService
    {
        private const string ListenerName = "CacheStoreServiceListener";
        private readonly Uri _serviceUri;
        private int _partitionCount = 1;
        private int _maxCacheSizeInMegaBytes = 1500;
        private IServiceFabricDistributedCacheService _storeService;
        private IServiceFabricDistributedCacheStoreBackgroundWorker _backgroundWorker;

        public DistributedCacheStatefulService(StatefulServiceContext context)
            : base(context)
        {
            _serviceUri = context.ServiceName;

            var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var value = configurationPackage.Settings.Sections["CacheConfig"].Parameters["MaxCacheSizeInMegabytes"].Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                _maxCacheSizeInMegaBytes = (int)Convert.ChangeType(value, typeof(int));
            }

            ServiceEventSource.Current.ServiceMessage(context, $"Max Cache size is {_maxCacheSizeInMegaBytes}");

            var svc = new DistributedCacheStore(
                this.StateManager,
                this.GetMaxCacheSizeInBytes(),
                (message) => ServiceEventSource.Current.ServiceMessage(context, message));

            _storeService = svc;
            _backgroundWorker = svc;
        }

        protected async override Task OnOpenAsync(
            ReplicaOpenMode openMode,
            CancellationToken cancellationToken)
        {
            var client = new FabricClient();
            _partitionCount = (await client.QueryManager.GetPartitionListAsync(_serviceUri)).Count;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            yield return new ServiceReplicaListener(context =>
                new FabricTransportServiceRemotingListener(context, this), ListenerName);
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            return _backgroundWorker.RunAsync(cancellationToken);
        }

        public byte[] Get(string key)
        {
            return _storeService.Get(key);
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _storeService.GetAsync(key, token);
        }

        public void Refresh(string key)
        {
            _storeService.Refresh(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _storeService.RefreshAsync(key, token);
        }

        public void Remove(string key)
        {
            _storeService.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return _storeService.RemoveAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _storeService.Set(key, value, options);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            return _storeService.SetAsync(key, value, options, token);
        }

        public Task<byte[]> CreateCachedItemAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token)
        {
            return _storeService.CreateCachedItemAsync(key, value, options, token);
        }

        private int GetMaxCacheSizeInBytes()
        {
            const int BytesInMegabyte = 1048576;

            return (_maxCacheSizeInMegaBytes * BytesInMegabyte) / _partitionCount;
        }
    }
}