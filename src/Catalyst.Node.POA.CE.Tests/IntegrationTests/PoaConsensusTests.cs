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
using System.Text.RegularExpressions;
using System.Threading;
using Autofac;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Catalyst.TestUtils;
using FluentAssertions;
using MultiFormats;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.POA.CE.Tests.IntegrationTests
{
    public sealed class PoaConsensusTests : FileSystemBasedTest
    {
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly ILifetimeScope _scope;
        private readonly List<PoaTestNode> _nodes;

        public PoaConsensusTests(ITestOutputHelper output) : base(output)
        {
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
                    var dfs = TestDfs.GetTestDfs(output, fileSystem, "ed25519");
                    return new {index = i, name, privateKey, nodeSettings, peerIdentifier, dfs, fileSystem};
                }
            ).ToList();

            var peerIdentifiers = poaNodeDetails.Select(n => n.peerIdentifier).ToList();

            _nodes = new List<PoaTestNode>();
            foreach (var nodeDetails in poaNodeDetails)
            {
                nodeDetails.dfs.Options.Discovery.BootstrapPeers = poaNodeDetails.Except(new[] {nodeDetails})
                   .Select(x => x.dfs.LocalPeer.Addresses.First());
                var node = new PoaTestNode(nodeDetails.name,
                    nodeDetails.privateKey,
                    nodeDetails.nodeSettings,
                    nodeDetails.dfs,
                    peerIdentifiers.Except(new[] {nodeDetails.peerIdentifier}),
                    nodeDetails.fileSystem,
                    output);

                _nodes.Add(node);
            }
        }

        [Fact]
        public void Run_ConsensusAsync()
        {
            _nodes.AsParallel()
               .ForAll(n =>
                {
                    n?.RunAsync(_endOfTestCancellationSource.Token);
                    n?.Consensus.StartProducing();
                });

            var autoResetEvent = new AutoResetEvent(false);
            bool autoResetEventResult;
            var multihashList = new List<MultiHash>();
            using (var watcher = new FileSystemWatcher())
            {
                watcher.Path = FileSystem.GetCatalystDataDir().FullName;
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Filter = "*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                watcher.Created += (source, e) =>
                {
                    var match = Regex.Match(e.FullPath, @"(blocks\\.*)");
                    if (match.Success)
                    {
                        var fileInfo = new FileInfo(e.FullPath);
                        multihashList.Add(new MultiHash(fileInfo.Name.FromBase32()));

                        if (multihashList.Count >= _nodes.Count())
                        {
                            autoResetEvent.Set();
                        }
                    }
                };

                autoResetEventResult = autoResetEvent.WaitOne(TimeSpan.FromSeconds(30));
            }

            autoResetEventResult.Should().Be(true);

            multihashList.Distinct().Count().Should().Be(1, "only the elected producer should score high enough to see his block elected.");

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
