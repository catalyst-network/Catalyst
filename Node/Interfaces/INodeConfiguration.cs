using ADL.Rpc.Server;

namespace ADL.Node.Interfaces
{
    public interface INodeConfiguration
    {
        IProtocolSettings Protocol { get; }
        IPathSettings Paths { get; }
        IP2PSettings P2P { get; }
        IRpcSettings RPC { get; }
    }
}
