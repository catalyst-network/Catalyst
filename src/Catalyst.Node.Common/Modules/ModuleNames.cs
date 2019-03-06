using System.Collections.Generic;
using Catalyst.Node.Common.Helpers;

namespace Catalyst.Node.Common.Modules
{
    public class ModuleName : Enumeration
    {
        public static readonly ModuleName Authentication = new AuthenticationType();
        public static readonly ModuleName Consensus = new ConsensusType();
        public static readonly ModuleName Contract = new ContractType();
        public static readonly ModuleName Dfs = new DfsType();
        public static readonly ModuleName Gossip = new GossipType();
        public static readonly ModuleName Ledger = new LedgerType();
        public static readonly ModuleName Mempool = new MempoolType();

        protected ModuleName(int id, string name) : base(id, name) {}
        private class AuthenticationType : ModuleName { public AuthenticationType() : base(1, "Authentication") { } }
        private class ConsensusType : ModuleName { public ConsensusType() : base(1, "Consensus") { } }
        private class ContractType : ModuleName { public ContractType() : base(1, "Contract") { } }
        private class DfsType : ModuleName { public DfsType() : base(1, "Dfs") { } }
        private class GossipType : ModuleName { public GossipType() : base(1, "Gossip") { } }
        private class LedgerType : ModuleName { public LedgerType() : base(1, "Ledger") { } }
        private class MempoolType : ModuleName { public MempoolType() : base(1, "Mempool") { } }
    }
}
