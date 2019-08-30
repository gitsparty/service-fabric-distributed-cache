using System.Threading.Tasks;

namespace SoCreate.DistributedCacheStoreService.ServiceFabric.Client
{
    public interface IDistributedCacheStoreLocator
    {
        Task<IServiceFabricCacheStoreService> GetCacheStoreProxy(string cacheKey);
    }
}