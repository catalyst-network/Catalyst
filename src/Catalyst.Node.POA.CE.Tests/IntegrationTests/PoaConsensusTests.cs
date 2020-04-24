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
using Catalyst.Abstractions.FileSystem;
using Catalyst.Core.Modules.Consensus.Cycle;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;


namespace Catalyst.Node.POA.CE.Tests.IntegrationTests
{
    public sealed class PoaConsensusTests : FileSystemBasedTest
    {
        private CancellationTokenSource _endOfTestCancellationSource;
        private ILifetimeScope _scope;
        private List<PoaTestNode> _nodes;

        [SetUp]
        public void Init()
        {
            this.Setup(TestContext.CurrentContext);

            ContainerProvider.ConfigureContainerBuilder(true, true, true);
            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);

            var context = new FfiWrapper();

            _endOfTestCancellationSource = new CancellationTokenSource();

            var poaNodeDetails = Enumerable.Range(0, 3).Select(i =>
            {
                var fileSystem = Substitute.For<IFileSystem>();
                var path = Path.Combine(FileSystem.GetCatalystDataDir().FullName, $"producer{i}");
                fileSystem.GetCatalystDataDir().Returns(new DirectoryInfo(path));

                var privateKey = context.GeneratePrivateKey();
                var publicKey = privateKey.GetPublicKey();
                var nodeSettings = PeerSettingsHelper.TestPeerSettings(publicKey.Bytes, 2000 + i);
                var peerIdentifier = nodeSettings.PeerId;
                var name = $"producer{i.ToString()}";
                var dfs = TestDfs.GetTestDfs(fileSystem);
                return new { index = i, name, privateKey, nodeSettings, peerIdentifier, dfs, fileSystem };
            }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = new List<PoaTestNode>();
            foreach (var nodeDetails in poaNodeDetails)
            {
                nodeDetails.dfs.Options.Discovery.BootstrapPeers = poaNodeDetails.Select(x => x.dfs.LocalPeer.Addresses.First());
                var node = new PoaTestNode(nodeDetails.name,
                    nodeDetails.privateKey,
                    nodeDetails.nodeSettings,
                    nodeDetails.dfs,
                    peerIdentifiers.Except(new[] { nodeDetails.peerIdentifier }),
                    nodeDetails.fileSystem);

                _nodes.Add(node);
            }
        }

        [Test]
        [Property(Traits.TestType, Traits.IntegrationTest)]
        public async Task Run_ConsensusAsync()
        {
            _nodes.AsParallel()
               .ForAll(n =>
                {
                    n?.RunAsync(_endOfTestCancellationSource.Token);
                    n?.Consensus.StartProducing();
                });

            await Task.Delay(Debugger.IsAttached
                    ? TimeSpan.FromHours(3)
                    : CycleConfiguration.Default.CycleDuration.Multiply(2.3))
               .ConfigureAwait(false);

            //At least one delta should be produced
            var maxDeltasProduced = 1;
            var files = new List<string>();
            for (var i = 0; i < _nodes.Count; i++)
            {
                var dfsDir = Path.Combine(FileSystem.GetCatalystDataDir().FullName, $"producer{i}/dfs", "blocks");
                var deltaFiles = Directory.GetFiles(dfsDir).Select(x => new FileInfo(x).Name).ToList();
                maxDeltasProduced = Math.Max(maxDeltasProduced, deltaFiles.Count());
                files.AddRange(deltaFiles);
            }

            files.Distinct().Count().Should().Be(maxDeltasProduced,
                "only the elected producer should score high enough to see his block elected. Found: " +
                files.Aggregate((x, y) => x + "," + y));

            _endOfTestCancellationSource.CancelAfter(TimeSpan.FromMinutes(3));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;

            if (_endOfTestCancellationSource.Token.IsCancellationRequested
             && _endOfTestCancellationSource.Token.CanBeCanceled)
                _endOfTestCancellationSource.Cancel();

            _endOfTestCancellationSource.Dispose();
            _nodes.AsParallel().ForAll(n => n.Dispose());

            _scope.Dispose();
        }
    }
}
