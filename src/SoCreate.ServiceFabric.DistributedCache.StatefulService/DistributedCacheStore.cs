using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Fabric;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Rt = Microsoft.ServiceFabric.Services.Runtime;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService
{
    internal sealed class DistributedCacheStore : Rt.StatefulService, DistributedCacheStoreService
    {
        private const string ListenerName = "CacheStoreServiceListener";
        private readonly Uri _serviceUri;
        private int _partitionCount = 1;
        private int _maxCacheSizeInMegaBytes = 1500;

        public DistributedCacheStore(StatefulServiceContext context)
            : base(context, (message) => ServiceEventSource.Current.ServiceMessage(context, message))
        {
            var configurationPackage = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            var value = configurationPackage.Settings.Sections["CacheConfig"].Parameters["MaxCacheSizeInMegabytes"].Value;

            if (!string.IsNullOrWhiteSpace(value))
            {
                _maxCacheSizeInMegaBytes = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        public DistributedCacheStore(
            StatefulServiceContext context,
            IReliableStateManagerReplica2 reliableStateManagerReplica,
            ISystemClock systemClock,
            Action<string> log)
            : base(context, reliableStateManagerReplica)
        {
            _serviceUri = context.ServiceName;
            _reliableStateManagerReplica = reliableStateManagerReplica;
            _log = log;
            _systemClock = systemClock;
        }

        protected override int MaxCacheSizeInMegabytes
        {
            get
            {
                return _maxCacheSizeInMegaBytes;
            }
        }

        protected async override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
        {
            var client = new FabricClient();
            _partitionCount = (await client.QueryManager.GetPartitionListAsync(_serviceUri)).Count;
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            yield return new ServiceReplicaListener(context =>
                new FabricTransportServiceRemotingListener(context, this), ListenerName);
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
        }
    }
}