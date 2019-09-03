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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Config;
using Catalyst.Core.Consensus.Cycle;
using Catalyst.Core.Cryptography;
using Catalyst.Core.P2P;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.IntegrationTests.Consensus
{
    public sealed class PoaConsensusTests : ConfigFileBasedTest
    {
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly ILifetimeScope _scope;
        private readonly List<PoaTestNode> _nodes;

        public PoaConsensusTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile)
        }, output)
        {
            ContainerProvider.ConfigureContainerBuilder(true, true);
            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);

            var context = new CryptoContext(new CryptoWrapper());

            _endOfTestCancellationSource = new CancellationTokenSource();

            var poaNodeDetails = Enumerable.Range(0, 3).Select(i =>
                {
                    var privateKey = context.GeneratePrivateKey();
                    var publicKey = privateKey.GetPublicKey();
                    var nodeSettings = PeerSettingsHelper.TestPeerSettings(publicKey.Bytes, 2000 + i);
                    var peerIdentifier = new PeerIdentifier(nodeSettings) as IPeerIdentifier;
                    var name = $"producer{i.ToString()}";
                    return new {index = i, name, privateKey, nodeSettings, peerIdentifier};
                }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = poaNodeDetails.Select(
                p => new PoaTestNode($"producer{p.index.ToString()}",
                    p.privateKey,
                    p.nodeSettings,
                    peerIdentifiers.Except(new[] {p.peerIdentifier}),
                    FileSystem,
                    output)).ToList();
        }

        [Fact]
        public async Task Run_Consensus()
        {
            _nodes.AsParallel()
               .ForAll(n =>
                {
                    n.RunAsync(_endOfTestCancellationSource.Token);
                    n.Consensus.StartProducing();
                });

            await Task.Delay(Debugger.IsAttached
                    ? TimeSpan.FromHours(3)
                    : CycleConfiguration.Default.CycleDuration.Multiply(1.3))
               .ConfigureAwait(false);

            var dfsDir = Path.Combine(FileSystem.GetCatalystDataDir().FullName, "dfs");
            Directory.GetFiles(dfsDir).Length.Should().Be(1);

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
            _nodes.AsParallel().ForAll(n => n.Dispose());

            _scope.Dispose();
        }
    }
}
