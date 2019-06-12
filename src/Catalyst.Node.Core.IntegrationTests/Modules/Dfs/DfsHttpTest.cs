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
using System.Net.Http;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.IntegrationTests.Modules.Dfs
{
    public sealed class DfsHttpTest : FileSystemBasedTest
    {
        private readonly IpfsAdapter _ipfs;
        private readonly Core.Modules.Dfs.Dfs _dfs;
        private readonly DfsHttp _dfsHttp;

        public DfsHttpTest(ITestOutputHelper output) : base(output)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new[]
            {
                "seed1.server.va",
                "island.domain.tv"
            });
            
            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            var logger = Substitute.For<ILogger>();
            _ipfs = new IpfsAdapter(passwordReader, peerSettings, FileSystem, logger);
            _dfs = new Core.Modules.Dfs.Dfs(_ipfs, logger);
            _dfsHttp = new DfsHttp(_ipfs);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Should_have_a_URL_for_content()
        {
            const string text = "good evening from IPFS!";
            var id = await _dfs.AddTextAsync(text);
            string url = _dfsHttp.ContentUrl(id);
            url.Should().StartWith("http");
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Should_serve_the_content()
        {
            const string text = "good afternoon from IPFS!";
            var id = await _dfs.AddTextAsync(text);
            string url = _dfsHttp.ContentUrl(id);

            var httpClient = new HttpClient();

            // The gateway takes some time to startup.
            var end = DateTime.Now.AddSeconds(10);
            string content = null;
            while (content != null && DateTime.Now < end)
            {
                try
                {
                    content = await httpClient.GetStringAsync(url);
                }
                catch (Exception)
                {
                    await Task.Delay(200);
                }
            }

            content.Should().Equals(text);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipfs?.Dispose();
                _dfsHttp?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
