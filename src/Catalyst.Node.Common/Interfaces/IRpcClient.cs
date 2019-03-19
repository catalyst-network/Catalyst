using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Shell;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IRpcClient
    {
         Task RunClientAsync();
         
         Task RunClientAsync(RpcNode node);
    }
}