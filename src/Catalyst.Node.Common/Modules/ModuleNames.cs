using System.Collections.Generic;
using Catalyst.Node.Common.Helpers;

namespace Catalyst.Node.Common.Modules
{
    public class ModuleName : Enumeration
    {
        public static ModuleName Consensus = new ConsensusType();
        public static ModuleName Contract = new ContractType();
        public static ModuleName Dfs = new DfsType();
        public static ModuleName Gossip = new GossipType();
        public static ModuleName Ledger = new LedgerType();
        public static ModuleName Mempool = new MempoolType();

        protected ModuleName(int id, string name) : base(id, name) {}

        private class ConsensusType : ModuleName { public ConsensusType() : base(1, "Consensus") { } }
        private class ContractType : ModuleName { public ContractType() : base(1, "Contract") { } }
        private class DfsType : ModuleName { public DfsType() : base(1, "Dfs") { } }
        private class GossipType : ModuleName { public GossipType() : base(1, "Gossip") { } }
        private class LedgerType : ModuleName { public LedgerType() : base(1, "Ledger") { } }
        private class MempoolType : ModuleName { public MempoolType() : base(1, "Mempool") { } }
    }
}
