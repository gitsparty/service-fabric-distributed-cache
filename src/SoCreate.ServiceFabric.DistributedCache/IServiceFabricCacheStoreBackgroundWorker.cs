using System.Threading;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public interface IServiceFabricCacheStoreBackgroundWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
