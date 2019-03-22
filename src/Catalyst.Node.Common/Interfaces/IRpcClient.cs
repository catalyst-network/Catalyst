using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Shell;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Node.Common.Interfaces
{
    public interface IRpcClient
    {       
         Task<ISocketClient> GetClientSocketAsync(IRpcNodeConfig nodeConfig);

         Task SendMessage(IRpcNode node, Any message);
    }
}