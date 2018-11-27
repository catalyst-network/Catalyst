using ADL.Cryptography.SSL;
using ADL.P2P;
using ADL.Rpc;

namespace ADL.Node.Interfaces
{
    public interface INodeConfiguration
    {
        IP2PSettings P2P { get; set; }
        ISslSettings Ssl { get; set; }
        IRpcSettings Rpc { get; set; }
        IDfsSettings Dfs { get; set; }
        IGossipSettings Gossip { get; set; }
        IMempoolSettings Mempool { get; set; }
        IContractSettings Contract { get; set; }
        IProtocolSettings Protocol { get; set; }
        IConsensusSettings Consensus { get; set; }
        NodeOptions NodeOptions { get; set; }
        IPersistanceSettings Persistance { get; set; }
    }
}
