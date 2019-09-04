using System.Threading;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.DistributedCache
{
    public interface IServiceFabricDistributedCacheStoreBackgroundWorker
    {
        Task RunAsync(CancellationToken cancellationToken);
    }
}
