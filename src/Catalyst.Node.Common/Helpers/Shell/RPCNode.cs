using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Shell
{
    public class RpcNode : IRpcNode
    {
        public IRpcNodeConfig Config { get; }
    
        public RpcNode(IRpcNodeConfig config, ISocketClient socketClient)
        {
            Config = config;
            SocketClient = socketClient;
        }

        public ISocketClient SocketClient { get; }
    }
}