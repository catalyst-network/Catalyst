using System.Threading;
using System.Threading.Tasks;

namespace ADL.Services
{
    public interface IAsyncService : IService
    {
        Task AwaitCancellation(CancellationToken token);

        Task RunServiceAsync(Server server,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}