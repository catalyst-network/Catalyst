using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Node.Common.Modules.Dfs
{
    public interface IDfs
    {
        Task StartAsync(CancellationToken cancellationToken = default);
        Task<string> AddFileAsync(string filename, CancellationToken cancellationToken = default);
        Task<string> ReadAllTextAsync(string filename, CancellationToken cancellationToken = default);
    }
}