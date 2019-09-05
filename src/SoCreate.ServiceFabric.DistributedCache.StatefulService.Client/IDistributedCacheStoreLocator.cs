using System.Threading.Tasks;
using SoCreate.ServiceFabric.DistributedCache.Abstractions;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public interface IDistributedCacheStoreLocator
    {
        Task<IServiceFabricDistributedCacheService> GetCacheStoreProxy(string cacheKey);
    }
}