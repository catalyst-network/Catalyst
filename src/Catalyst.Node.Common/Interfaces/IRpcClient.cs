using System.Threading.Tasks;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IRpcClient
    {
         Task RunClientAsync();
    }
}