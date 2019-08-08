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

using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.Modules.Consensus.Cycle;
using Catalyst.Protocol;
using Catalyst.TestUtils;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Serilog;

namespace Catalyst.Core.Lib.UnitTests.Modules.Consensus
{
    public class ConsensusTests
    {
        private readonly IDeltaBuilder _deltaBuilder;
        private readonly IDeltaVoter _deltaVoter;
        private readonly IDeltaElector _deltaElector;
        private readonly IDeltaCache _deltaCache;
        private readonly IDeltaHub _deltaHub;
        private readonly ILogger _logger;
        private readonly TestCycleEventProvider _cycleEventProvider;
        private readonly Lib.Modules.Consensus.Consensus _consensus;
        private readonly Multihash _previousDeltaHash;

        public ConsensusTests()
        {
            _cycleEventProvider = new TestCycleEventProvider();
            _deltaBuilder = Substitute.For<IDeltaBuilder>();
            _deltaVoter = Substitute.For<IDeltaVoter>();
            _deltaElector = Substitute.For<IDeltaElector>();
            _deltaCache = Substitute.For<IDeltaCache>();
            _deltaHub = Substitute.For<IDeltaHub>();
            _logger = Substitute.For<ILogger>();
            _consensus = new Lib.Modules.Consensus.Consensus(
                _deltaBuilder, 
                _deltaVoter, 
                _deltaElector, 
                _deltaCache,
                _deltaHub, 
                _cycleEventProvider, 
                _logger);

            _previousDeltaHash = "previousDelta".ToUtf8ByteString().ComputeMultihash(new BLAKE2B_256());
        }

        public void StartProducing_Should_Trigger_BuildDeltaCandidate_On_Construction_Producing_Phase()
        {
            _cycleEventProvider.MovePastNextPhase(PhaseName.Construction);
        }
    }
}
