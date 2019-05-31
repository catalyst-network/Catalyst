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
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Core.Modules.Dfs;
using FluentAssertions;
using NSubstitute;
using Polly;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.UnitTests.Modules.Dfs
{
    public sealed class IpfsDfsLiveTests : FileSystemBasedTest
    {
        private readonly IpfsAdapter _ipfs;
        private readonly ILogger _logger;

        public IpfsDfsLiveTests(ITestOutputHelper output) : base(output)
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new[]
            {
                "seed1.server.va",
                "island.domain.tv"
            });
            
            var passwordReader = Substitute.For<IPasswordReader>();
            passwordReader.ReadSecurePassword().ReturnsForAnyArgs(TestPasswordReader.BuildSecureStringPassword("abcd"));
            _logger = Substitute.For<ILogger>();
            _ipfs = new IpfsAdapter(passwordReader, peerSettings, FileSystem, _logger);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_text()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            var linearBackOffRetryPolicy = Policy.Handle<TaskCanceledException>()
               .WaitAndRetryAsync(5, retryAttempt =>
                {
                    var timeSpan = TimeSpan.FromMilliseconds(retryAttempt + 5);
                    cts = new CancellationTokenSource(timeSpan);
                    return timeSpan;
                });

            const string text = "good morning";
            var dfs = new Core.Modules.Dfs.Dfs(_ipfs, _logger);
            var id = await linearBackOffRetryPolicy.ExecuteAsync(
                () => dfs.AddTextAsync(text, cts.Token)
            );
            var content = await linearBackOffRetryPolicy.ExecuteAsync(
                () => dfs.ReadTextAsync(id, cts.Token)
            );
            content.Should().Be(text);
        }

        [Fact]
        [Trait(Traits.TestType, Traits.IntegrationTest)]
        public async Task DFS_should_add_and_read_binary()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var binary = new byte[]
            {
                1, 2, 3
            };
            var ms = new MemoryStream(binary);
            var dfs = new Core.Modules.Dfs.Dfs(_ipfs, _logger);
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
                _ipfs?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
