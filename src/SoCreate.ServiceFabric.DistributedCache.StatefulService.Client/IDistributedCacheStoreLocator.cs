using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public interface IDistributedCacheStoreLocator
    {
        Task<IServiceFabricDistributedCacheService> GetCacheStoreProxy(string cacheKey);
    }
}