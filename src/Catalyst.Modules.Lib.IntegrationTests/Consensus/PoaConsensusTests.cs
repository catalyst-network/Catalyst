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
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Cryptography;
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;
using Catalyst.Cryptography.BulletProofs.Wrapper;
using Catalyst.TestUtils;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    public class PoaConsensusTests : ConfigFileBasedTest
    {
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;
        private readonly List<PoaTestNode> _nodes;

        public PoaConsensusTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile)
        }, output)
        {
            ContainerProvider.ConfigureContainerBuilder(true, true, true);
            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _logger = _scope.Resolve<ILogger>();

            var context = new CryptoContext(new CryptoWrapper());

            _endOfTestCancellationSource = new CancellationTokenSource();

            var poaNodeDetails = Enumerable.Range(0, 3).Select(i =>
                {
                    var privateKey = context.GeneratePrivateKey();
                    var publicKey = privateKey.GetPublicKey();
                    var nodeSettings = PeerSettingsHelper.TestPeerSettings(publicKey.Bytes.KeyToString(), port: 2000 + i);
                    var peerIdentifier = new PeerIdentifier(nodeSettings) as IPeerIdentifier;
                    var name = $"producer{i}";
                    return new {index = i, name, privateKey, nodeSettings, peerIdentifier};
                }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = poaNodeDetails.Select(
                p => new PoaTestNode($"producer{p.index}",
                    p.privateKey,
                    p.nodeSettings,
                    peerIdentifiers.Except(new[] {p.peerIdentifier}), 
                    FileSystem, 
                    output)).ToList();
        }

        [Fact]
        public async Task Run_Consensus()
        {
            var observer = Observer.Create<IPhase>(ObservedPhase);
            _nodes.AsParallel()
               .ForAll(async n =>
                {
                    n.RunAsync(_endOfTestCancellationSource.Token);
                });

            await Task.Delay(TimeSpan.FromSeconds(20));

            _endOfTestCancellationSource.CancelAfter(TimeSpan.FromMinutes(3));
        }

        private void ObservedPhase(IPhase phase)
        {
            _logger.Debug(phase.ToString());
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

