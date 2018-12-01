using ADL.Peer;
using ADL.Rpc;
using ADL.DFS;
using ADL.Mempool;
using ADL.Gossip;
using ADL.Contract;
using ADL.Consensus;
using ADL.Ledger;

namespace ADL.Node
{
    public interface ISettings
    {
        IPeerSettings Peer { get; set; }
        IRpcSettings Rpc { get; set; }
        IDfsSettings Dfs { get; set; }
        IGossipSettings Gossip { get; set; }
        IMempoolSettings Mempool { get; set; }
        IContractSettings Contract { get; set; }
        IConsensusSettings Consensus { get; set; }
        NodeOptions NodeOptions { get; set; }
        ILedgerSettings Ledger { get; set; }
    }
}
