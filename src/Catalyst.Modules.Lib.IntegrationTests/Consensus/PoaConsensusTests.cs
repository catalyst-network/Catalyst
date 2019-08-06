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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    public class PoaConsensusTests : FileSystemBasedTest
    {
        private readonly IDictionary<IPeerIdentifier, PoaTestNode> _nodesById;
        private readonly CancellationTokenSource _endOfTestCancellationSource;

        public PoaConsensusTests(ITestOutputHelper output) : base(output)
        {
            _endOfTestCancellationSource = new CancellationTokenSource();

            var peerSettings = Enumerable.Range(0, 3).Select(i =>
                PeerSettingsHelper.TestPeerSettings($"producer{i}", port: 2000 + i)
            ).ToList();

            var peerIdentifiers = peerSettings
               .Select(p => new PeerIdentifier(p) as IPeerIdentifier)
               .ToList();

            _nodesById = peerSettings.Select((p, i) => new {Settings = p, Index = i, Identifier = new PeerIdentifier(p) as IPeerIdentifier})
               .ToDictionary(
                    p => p.Identifier, 
                    p => new PoaTestNode($"producer{p.Index}", p.Settings, peerIdentifiers.Except(new[] {p.Identifier}), FileSystem, output));
        }

        [Fact]
        public async Task Run_Consensus()
        {
            var observer = Observer.Create<IPhase>(ObservedPhase);
            _nodesById.Values.AsParallel()
               .ForAll(async n =>
                {
                    n.Consensus.CycleEventsProvider.PhaseChanges.Subscribe(observer);
                    n.RunAsync(_endOfTestCancellationSource.Token);
                });

            await Task.Delay(TimeSpan.FromSeconds(5));

            _endOfTestCancellationSource.CancelAfter(TimeSpan.FromMinutes(3));
        }

        private void ObservedPhase(IPhase phase)
        {
            Output.WriteLine(phase.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            if (_endOfTestCancellationSource.Token.IsCancellationRequested
             && _endOfTestCancellationSource.Token.CanBeCanceled)
            {
                _endOfTestCancellationSource.Cancel();
            }

            _endOfTestCancellationSource.Dispose();
            _nodesById.Values.AsParallel().ForAll(n => n.Dispose());
        }
    }
}

