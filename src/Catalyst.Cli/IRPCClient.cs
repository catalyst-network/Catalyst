using System.Threading;
using System.Threading.Tasks;

namespace Catalyst.Cli
{
    public interface IRPCClient
    {
         Task RunClientAsync();
    }
}