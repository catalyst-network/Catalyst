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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Node.Core.Modules.Dfs;
using FluentAssertions;
using Serilog;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Cryptography;
using Polly;
using Polly.Retry;

namespace Catalyst.Node.Core.UnitTest.Modules.Dfs
{
    public sealed class IpfsDfsLiveTests : FileSystemBasedTest
    {
        private readonly IIpfsEngine _ipfsEngine;
        private readonly ILogger _logger;
        private readonly IPeerSettings _peerSettings;
        private readonly IPasswordReader _passwordReader;
        private readonly AsyncRetryPolicy _exponentialBackOffRetryPolicy;

        public IpfsDfsLiveTests(ITestOutputHelper output) : base(output)
        {
            _peerSettings = Substitute.For<IPeerSettings>();
            _peerSettings.SeedServers.Returns(new[] {"seed1.server.va", "island.domain.tv"});
            _passwordReader = Substitute.For<IPasswordReader>();
            _passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            _logger = Substitute.For<ILogger>();
            _ipfsEngine = new IpfsEngine(_passwordReader, _peerSettings, FileSystem, _logger);
            _exponentialBackOffRetryPolicy = Policy.Handle<TaskCanceledException>()
               .WaitAndRetryAsync(5, retryAttempt =>
                    TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt))
                );
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public void DFS_should_add_and_read_text()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            const string text = "good morning";
            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            var id = _exponentialBackOffRetryPolicy.ExecuteAsync(
                () => dfs.AddTextAsync(text, cts.Token)
            ).Result;
            var content = _exponentialBackOffRetryPolicy.ExecuteAsync(
                () => dfs.ReadTextAsync(id, cts.Token)
            ).Result;
            content.Should().Be(text);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_binary()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var binary = new byte[] {1, 2, 3};
            var ms = new MemoryStream(binary);
            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            var id = await dfs.AddAsync(ms, "", cts.Token);
            using (var stream = await dfs.ReadAsync(id, cts.Token))
            {
                var content = new byte[binary.Length];
                stream.Read(content, 0, content.Length);
                content.Should().Equal(binary);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ipfsEngine?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
