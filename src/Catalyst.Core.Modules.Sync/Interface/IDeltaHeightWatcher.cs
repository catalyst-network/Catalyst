using System.Threading.Tasks;
using Catalyst.Core.Lib.DAO.Ledger;

namespace Catalyst.Core.Modules.Sync.Interface
{
    public interface IDeltaHeightWatcher
    {
        DeltaIndexDao LatestDeltaHash { get; }
        Task StartAsync();
        Task StopAsync();
        Task WaitForDeltaHeightAsync(int currentDeltaIndex);
    }
}
