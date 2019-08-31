using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache.StatefulService.Client
{
    public interface IDistributedCacheStoreLocator
    {
        Task<IServiceFabricCacheStoreService> GetCacheStoreProxy(string cacheKey);
    }
}