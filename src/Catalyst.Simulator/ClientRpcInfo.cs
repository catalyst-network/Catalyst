using Catalyst.Common.Interfaces.P2P;
using Catalyst.Simulator.Interfaces;

namespace Catalyst.Simulator
{
    public class ClientRpcInfo
    {
        public ClientRpcInfo(IPeerIdentifier peerIdentifier, IRpcClient rpcClient)
        {
            PeerIdentifier = peerIdentifier;
            RpcClient = rpcClient;
        }

        public IPeerIdentifier PeerIdentifier { private set; get; }
        public IRpcClient RpcClient { private set; get; }
    }
}
