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
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Types;
using Catalyst.Core.Lib.Modules.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests.Modules.Dfs
{
    public sealed class DfsHttpTests : FileSystemBasedTest
    {
        private readonly IpfsAdapter _ipfs;
        private readonly Catalyst.Core.Lib.Modules.Dfs.Dfs _dfs;
        private readonly DfsHttp _dfsHttp;

        public DfsHttpTests(ITestOutputHelper output) : base(output)
        {
            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePasswordAndAddToRegistry(Arg.Any<PasswordRegistryTypes>(), Arg.Any<string>()).ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            var logger = Substitute.For<ILogger>();
            _ipfs = new IpfsAdapter(passwordReader, FileSystem, logger);
            _dfs = new Catalyst.Core.Lib.Modules.Dfs.Dfs(_ipfs, logger);
            _dfsHttp = new DfsHttp(_ipfs);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task Should_have_a_URL_for_content()
        {
            const string text = "good evening from IPFS!";
            var id = await _dfs.AddTextAsync(text).ConfigureAwait(false);
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
            var end = DateTime.UtcNow.AddSeconds(10);
            string content = null;
            while (content != null && DateTime.UtcNow < end)
            {
                try
                {
                    content = await httpClient.GetStringAsync(url);
                }
                catch (Exception)
                {
                    await Task.Delay(200).ConfigureAwait(false);
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
