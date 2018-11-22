using ADL.Rpc;

namespace ADL.Node.Interfaces
{
    internal interface INodeConfiguration
    {
        IProtocolSettings Protocol { get; }
        IPersistanceSettings Persistance { get; }
        IP2PSettings P2P { get; }
        IRpcSettings Rpc { get; }
        IDfsSettings Dfs { get; }
    }
}
