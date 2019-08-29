#region LICENSE

/**
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

#endregion

using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Abstractions.Types
{
    public class ModuleTypes : Enumeration
    {
        public static readonly ModuleTypes Consensus = new ConsensusType();
        public static readonly ModuleTypes Contract = new ContractType();
        public static readonly ModuleTypes Dfs = new DfsType();
        public static readonly ModuleTypes DfsHttp = new DfsHttpType();
        public static readonly ModuleTypes Ledger = new LedgerType();
        public static readonly ModuleTypes KeySigner = new KeySignerType();
        public static readonly ModuleTypes Mempool = new MempoolType();
        private ModuleTypes(int id, string name) : base(id, name) { }

        private sealed class ConsensusType : ModuleTypes
        {
            public ConsensusType() : base(1, "Consensus") { }
        }

        private sealed class ContractType : ModuleTypes
        {
            public ContractType() : base(1, "Contract") { }
        }

        private sealed class DfsType : ModuleTypes
        {
            public DfsType() : base(1, "Dfs") { }
        }

        private sealed class DfsHttpType : ModuleTypes
        {
            public DfsHttpType() : base(1, "DfsHttp") { }
        }

        private sealed class LedgerType : ModuleTypes
        {
            public LedgerType() : base(1, "Ledger") { }
        }

        private sealed class KeySignerType : ModuleTypes
        {
            public KeySignerType() : base(1, "KeySigner") { }
        }

        private sealed class MempoolType : ModuleTypes
        {
            public MempoolType() : base(1, "Mempool") { }
        }
    }
}
