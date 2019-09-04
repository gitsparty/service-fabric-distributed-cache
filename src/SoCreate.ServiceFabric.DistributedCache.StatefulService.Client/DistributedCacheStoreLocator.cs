using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using System;
using System.Collections.Concurrent;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public class DistributedCacheStoreLocator : IDistributedCacheStoreLocator
    {
        private string _serviceUri;
        private readonly string _endpointName;
        private readonly FabricClient _fabricClient;
        private ServicePartitionList _partitionList;
        private readonly ConcurrentDictionary<Guid, IServiceFabricDistributedCacheService> _cacheStores;

        public DistributedCacheStoreLocator(
            IOptions<ServiceFabricCacheOptions> options)
        {
            var fabricOptions = options.Value;
            _serviceUri = fabricOptions.CacheStoreServiceUri;
            _endpointName = fabricOptions.CacheStoreEndpointName;
                       
            _fabricClient = new FabricClient();
            _cacheStores = new ConcurrentDictionary<Guid, IServiceFabricDistributedCacheService>();
        }

        public async Task<IServiceFabricDistributedCacheService> GetCacheStoreProxy(string cacheKey)
        {
            // Try to locate a cache store if one is not configured
            if (_serviceUri == null || _serviceUri.Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                _serviceUri = await LocateCacheStoreAsync();
                if (_serviceUri == null)
                {
                    throw new CacheStoreNotFoundException("Cache store not found in Service Fabric cluster.  Try setting the 'CacheStoreServiceUri' configuration option to the location of your cache store.");
                }
            }

            var partitionInformation = await GetPartitionInformationForCacheKey(cacheKey);

            return _cacheStores.GetOrAdd(partitionInformation.Id, key => {
                var info = (Int64RangePartitionInformation)partitionInformation;
                var resolvedPartition = new ServicePartitionKey(info.LowKey);

                var proxyFactory = new ServiceProxyFactory((c) =>
                {
                    return new FabricTransportServiceRemotingClientFactory();
                });

                return proxyFactory.CreateServiceProxy<IServiceFabricDistributedCacheService>(
                    new Uri(_serviceUri),
                    resolvedPartition,
                    TargetReplicaSelector.Default,
                    _endpointName);
            });
        }

        private async Task<ServicePartitionInformation> GetPartitionInformationForCacheKey(string cacheKey)
        {
            var md5 = MD5.Create();
            var value = md5.ComputeHash(Encoding.ASCII.GetBytes(cacheKey));
            var key = BitConverter.ToInt64(value, 0);

            if (_partitionList == null)
            {
                _partitionList = await _fabricClient.QueryManager.GetPartitionListAsync(new Uri(_serviceUri));
            }

            var partition = _partitionList.Single(p => ((Int64RangePartitionInformation)p.PartitionInformation).LowKey <= key && ((Int64RangePartitionInformation)p.PartitionInformation).HighKey >= key);
            return partition.PartitionInformation;
        }

        private async Task<string> LocateCacheStoreAsync()
        {
            try
            {
                bool hasPages = true;
                var query = new ApplicationQueryDescription() { MaxResults = 50 };

                while (hasPages)
                {
                    var apps = await _fabricClient.QueryManager.GetApplicationPagedListAsync(query);

                    query.ContinuationToken = apps.ContinuationToken;

                    hasPages = !string.IsNullOrEmpty(query.ContinuationToken);

                    foreach (var app in apps)
                    {
                        var serviceName = await LocateCacheStoreServiceInApplicationAsync(app.ApplicationName);
                        if (serviceName != null)
                            return serviceName.ToString();
                    }
                }
            }
            catch { }

            return null;
        }

        private async Task<Uri> LocateCacheStoreServiceInApplicationAsync(Uri applicationName)
        {
            try
            {
                bool hasPages = true;
                var query = new ServiceQueryDescription(applicationName) {};

                while (hasPages)
                {
                    var services = await _fabricClient.QueryManager.GetServicePagedListAsync(query);

                    query.ContinuationToken = services.ContinuationToken;

                    hasPages = !string.IsNullOrEmpty(query.ContinuationToken);

                    foreach (var service in services)
                    {
                        var found = IsCacheStore(service.ServiceName, service.ServiceTypeName);
                        if (found)
                        {
                            return service.ServiceName;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private bool IsCacheStore(Uri serviceName, string serviceTypeName)
        {
            try
            {
                if (this._serviceUri == null || this._serviceUri.Equals("*", StringComparison.OrdinalIgnoreCase))
                {
                    if (serviceTypeName.Equals("SoCreate.ServiceFabric.DistributedCache.StatefulService"))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
