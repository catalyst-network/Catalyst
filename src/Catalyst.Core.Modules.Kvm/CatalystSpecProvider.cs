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

using Catalyst.Abstractions.Types;
using MongoDB.Driver;
using Nethermind.Core.Specs;
using Nethermind.Int256;

namespace Catalyst.Core.Modules.Kvm
{
    public sealed class CatalystSpecProvider : ISpecProvider
    {
        public IReleaseSpec GenesisSpec => CatalystGenesisSpec.Instance;
        public IReleaseSpec GetSpec(long blockNumber) { return GenesisSpec; }
        public ForkActivation? MergeBlockNumber { get; } // TODO
        public ulong TimestampFork { get; } // TODO
        public UInt256? TerminalTotalDifficulty { get; } // TODO
        public ulong NetworkId { get; } // TODO
        public ulong ChainId => (ulong)NetworkTypes.Dev.Id; // @TODO should we not be using protocol.common.network?
        public ForkActivation[] TransitionActivations { get; } // TODO

        public IReleaseSpec GetSpec(ForkActivation forkActivation)
        {
            return null; // TODO
        }
        public long? DaoBlockNumber => null;
        public long[] TransitionBlocks { get; } = {0};

        public void UpdateMergeTransitionInfo(long? blockNumber, UInt256? terminalTotalDifficulty = null)
        {
            // TODO
        }
    }
}
