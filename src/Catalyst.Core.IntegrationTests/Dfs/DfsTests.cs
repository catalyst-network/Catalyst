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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.IntegrationTests.Dfs
{
    public sealed class DfsTests : FileSystemBasedTest
    {
        private readonly IpfsAdapter _ipfs;
        private readonly ILogger _logger;
        private readonly ITestOutputHelper _output;

        public DfsTests(ITestOutputHelper output) : base(output)
        {
            _output = output;
            var passwordReader = Substitute.For<IPasswordManager>();
            passwordReader.RetrieveOrPromptAndAddPasswordToRegistry(Arg.Any<PasswordRegistryTypes>(), Arg.Any<string>()).Returns(TestPasswordReader.BuildSecureStringPassword("abcd"));

            _logger = Substitute.For<ILogger>();
            _ipfs = new IpfsAdapter(passwordReader, FileSystem, _logger);

            // Starting IPFS takes a few seconds.  Do it here, so that individual
            // test times are not affected.
            _ipfs.Generic.IdAsync().Wait();
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_text()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            const string text = "good morning";
            var dfs = new Core.Dfs.Dfs(_ipfs, _logger);
            var id = await dfs.AddTextAsync(text, cts.Token);
            var content = await dfs.ReadTextAsync(id, cts.Token);

            content.Should().Be(text);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_binary()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var binary = new byte[]
            {
                1, 2, 3
            };
            var ms = new MemoryStream(binary);
            var dfs = new Core.Dfs.Dfs(_ipfs, _logger);
            var id = await dfs.AddAsync(ms, "", cts.Token);
            using (var stream = await dfs.ReadAsync(id, cts.Token))
            {
                var content = new byte[binary.Length];
                stream.Read(content, 0, content.Length);
                content.Should().Equal(binary);
            }
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_connect_to_a_seednode()
        {
            var seeds = (await _ipfs.Bootstrap.ListAsync().ConfigureAwait(false))
               .Select(a => a.PeerId)
               .ToArray();
            Assert.True(seeds.Length > 0, "no seed nodes defined");

            // Wait for a connection to a seed node.
            var start = DateTime.Now;
            var end = DateTime.Now.AddSeconds(15);
            var found = false;
            while (!found)
            {
                Assert.True(DateTime.Now <= end, "timeout");
                var peers = await _ipfs.Swarm.PeersAsync().ConfigureAwait(false);
                found = peers.Any(p => seeds.Contains(p.Id));
                await Task.Delay(100).ConfigureAwait(false);
            }

            _output.WriteLine($"Found in {(DateTime.Now - start).TotalSeconds.ToString()} seconds.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipfs?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
