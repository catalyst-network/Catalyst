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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.P2P;
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

            var peerIdentifiers = Enumerable.Range(0, 3).Select(i => 
                PeerIdentifierHelper.GetPeerIdentifier($"producer{i}")).ToList();

            _nodesById = peerIdentifiers.ToDictionary(
                p => p,
                p => BuildPoaTestNode(output, p, peerIdentifiers));
        }

        private static PoaTestNode BuildPoaTestNode(ITestOutputHelper output,
            IPeerIdentifier p,
            List<IPeerIdentifier> peerIdentifiers)
        {
            var node = new PoaTestNode(p, peerIdentifiers.Except(new[] {p}), output);
            node.BuildNode();
            return node;
        }

        [Fact]
        public async Task Run_Consensus()
        {
            _nodesById.Values.AsParallel()
               .ForAll(n => n.RunAsync(_endOfTestCancellationSource.Token));

            _endOfTestCancellationSource.CancelAfter(TimeSpan.FromMinutes(3));
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

