using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Shell;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IRpcClient
    {       
         Task RunClientAsync(RpcNode node);

         Task SendMessage(RpcNode node, object message);

         bool IsConnectedNode(string nodeId);
         
         RpcNode GetConnectedNode(string nodeId);
    }
}