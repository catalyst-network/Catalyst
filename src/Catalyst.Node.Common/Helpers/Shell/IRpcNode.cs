using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Shell {
    public interface IRpcNode {
        IRpcNodeConfig Config { get; }
        ISocketClient SocketClient { get; }
    }
}