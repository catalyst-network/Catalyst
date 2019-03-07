/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
 *
 * Catalyst.Node is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Node is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

﻿using Catalyst.Node.Common.Helpers.Enumerator;

namespace Catalyst.Node.Common.Modules
{
    public class ModuleName : Enumeration
    {
        public static readonly ModuleName Consensus = new ConsensusType();
        public static readonly ModuleName Contract = new ContractType();
        public static readonly ModuleName Dfs = new DfsType();
        public static readonly ModuleName Gossip = new GossipType();
        public static readonly ModuleName Ledger = new LedgerType();
        public static readonly ModuleName KeySigner = new KeySignerType();
        public static readonly ModuleName Mempool = new MempoolType();
        public static readonly ModuleName Fran = new FranType();

        private ModuleName(int id, string name) : base(id, name) { }

        private class ConsensusType : ModuleName
        {
            public ConsensusType() : base(1, "Consensus") { }
        }

        private class ContractType : ModuleName
        {
            public ContractType() : base(1, "Contract") { }
        }

        private class DfsType : ModuleName
        {
            public DfsType() : base(1, "Dfs") { }
        }

        private class GossipType : ModuleName
        {
            public GossipType() : base(1, "Gossip") { }
        }

        private class LedgerType : ModuleName
        {
            public LedgerType() : base(1, "Ledger") { }
        }
        
        private class KeySignerType : ModuleName
        {
            public KeySignerType() : base(1, "KeySigner") { }
        }

        private class MempoolType : ModuleName
        {
            public MempoolType() : base(1, "Mempool") { }
        }
        
        private class FranType : ModuleName
        {
            public FranType() : base(1, "Fran") { }
        }
    }
}