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
using System.Threading.Tasks;
using Catalyst.Node.Core.Modules.Consensus;
using Catalyst.Node.Core.Modules.Consensus.Cycle;
using Catalyst.TestUtils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.Modules.Consensus.Cycle
{
    public class CycleEventsProviderTests
    {
        private readonly ITestOutputHelper _output;

        public CycleEventsProviderTests(ITestOutputHelper output) { _output = output; }

        [Fact]
        public async Task Changes_should_happen_in_time()
        {
            _output.WriteLine($"starting at {DateTime.Now:ss:fff}");

            var cycleProvider = new CycleEventsProvider(TestCycleConfiguration.TestDefault, new DateTimeProvider());

            var tenEvents = cycleProvider.PhaseChanges;
            var counter = 0;
            var completed = false;

            tenEvents.Subscribe(p =>
            {
                counter++;
                _output.WriteLine($"{DateTime.Now:ss:fff} -- {p}");
            }, () => completed = true);

            await TaskHelper.WaitForAsync(() => counter >= 20, TimeSpan.FromSeconds(100));
            cycleProvider.Close();

            await TaskHelper.WaitForAsync(() => completed, TimeSpan.FromSeconds(10));

            _output.WriteLine($"completed at {DateTime.Now:ss:fff}");
        }
    }
}
