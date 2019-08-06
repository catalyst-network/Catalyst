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
using Catalyst.Common.Interfaces.Modules.Consensus.Cycle;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.TestUtils;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    public class PoaConsensusTests : ConfigFileBasedTest
    {
        private readonly IDictionary<IPeerIdentifier, PoaTestNode> _nodesById;
        private readonly CancellationTokenSource _endOfTestCancellationSource;
        private readonly List<IPeerSettings> _nodeSettings;
        private readonly ILifetimeScope _scope;
        private readonly ILogger _logger;

        public PoaConsensusTests(ITestOutputHelper output) : base(new[]
        {
            Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile)
        }, output)
        {
            ContainerProvider.ConfigureContainerBuilder(true, true, true);
            _scope = ContainerProvider.Container.BeginLifetimeScope(CurrentTestName);
            _logger = _scope.Resolve<ILogger>();

            _endOfTestCancellationSource = new CancellationTokenSource();

            _nodeSettings = Enumerable.Range(0, 3).Select(i =>
                PeerSettingsHelper.TestPeerSettings($"producer{i}", port: 2000 + i)
            ).ToList();

            var peerIdentifiers = _nodeSettings
               .Select(p => new PeerIdentifier(p) as IPeerIdentifier)
               .ToList();

            _nodesById = _nodeSettings.Select((p, i) => new {Settings = p, Index = i, Identifier = new PeerIdentifier(p) as IPeerIdentifier})
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

            await Task.Delay(TimeSpan.FromSeconds(20));

            //var transaction = TransactionHelper.GetTransaction(1, _nodesById[0].)
            //{
                
            //}

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
            _nodesById.Values.AsParallel().ForAll(n => n.Dispose());

            _scope.Dispose();
        }
    }
}

