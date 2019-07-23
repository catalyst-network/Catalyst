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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Core.Lib.IntegrationTests;
using Catalyst.Modules.Lib.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Consensus
{
    public class PoaConsensusTests : FileSystemBasedTest
    {
        private readonly FileSystemDfsTestNode _producer1;
        private readonly FileSystemDfsTestNode _producer2;

        public PoaConsensusTests(ITestOutputHelper output) : base(output)
        {
            _producer1 = new FileSystemDfsTestNode("producer1", Output);
            _producer2 = new FileSystemDfsTestNode("producer2", Output);
        }

        [Fact]
        public async Task MyTestedMethod_Should_Be_Producing_This_Result_When_Some_Conditions_Are_Met()
        {
            var producerCancellationToken = new CancellationToken();

            _producer1.RunAsync(producerCancellationToken);
            _producer2.RunAsync(producerCancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
            {
                return;
            }

            _producer1?.Dispose();
            _producer2?.Dispose();
        }
    }
}

