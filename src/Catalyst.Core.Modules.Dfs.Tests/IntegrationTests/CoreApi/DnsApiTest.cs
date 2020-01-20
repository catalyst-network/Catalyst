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
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Modules.Dfs.Tests.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Modules.Dfs.Tests.IntegrationTests.CoreApi
{
    public class DnsApiTest
    {
        private IDfsService ipfs;
        private ITestOutputHelper testOutput;

        public DnsApiTest(ITestOutputHelper output)
        {
            testOutput = output;
            ipfs = TestDfs.GetTestDfs(output);  
        }

        [Fact]
        public async Task Resolve()
        {
            ipfs = TestDfs.GetTestDfs(testOutput);

            var path = await ipfs.DnsApi.ResolveAsync("ipfs.io");
            Assert.NotNull(path);
        }

        [Fact]
        public void Resolve_NoLink()
        {
            ExceptionAssert.Throws<Exception>(() =>
            {
                var _ = ipfs.DnsApi.ResolveAsync("google.com").Result;
            });
        }

        [Fact]
        public async Task Resolve_Recursive()
        {
            var path = await ipfs.DnsApi.ResolveAsync("ipfs.io", true);
            Assert.StartsWith("/ipfs/", path);
        }
    }
}
