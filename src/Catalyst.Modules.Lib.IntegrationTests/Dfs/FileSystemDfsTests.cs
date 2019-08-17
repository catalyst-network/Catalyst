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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Modules.Lib.Dfs;
using Catalyst.TestUtils;
using FluentAssertions;
using Ipfs.Registry;
using Multiformats.Hash.Algorithms;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Modules.Lib.IntegrationTests.Dfs
{
    public class FileSystemDfsTests : FileSystemBasedTest
    {
        private readonly IDfs _dfs;
        private readonly CancellationToken _cancellationToken;

        public FileSystemDfsTests(ITestOutputHelper output) : base(output)
        {
            _cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(200)).Token;
            var hashingAlgorithm = HashingAlgorithm.All.First(x => x.Name == "blake2b-256");
            _dfs = new FileSystemDfs(FileSystem, hashingAlgorithm);
        }

        [Fact]
        public async Task AddTextAsync_Can_Be_Retrieved_With_ReadTextAsync()
        {
            var content = "Lorem Ipsum or something";
            var fileName = await _dfs.AddTextAsync(content, _cancellationToken);

            Thread.Sleep(100);

            var retrievedContent = await _dfs.ReadTextAsync(fileName, _cancellationToken);

            retrievedContent.Should().Be(content);
        }

        [Fact]
        public async Task AddAsync_Can_Be_Retrieved_With_ReadAsync()
        {
            var content = BitConverter.GetBytes(123456);
            var fileName = await _dfs.AddAsync(content.ToMemoryStream(), cancellationToken: _cancellationToken);

            Thread.Sleep(100);

            var retrievedContent = await _dfs.ReadAsync(fileName, _cancellationToken);
            var fileContent = await retrievedContent.ReadAllBytesAsync(_cancellationToken);

            fileContent.Should().BeEquivalentTo(content);
        }
    }
}
