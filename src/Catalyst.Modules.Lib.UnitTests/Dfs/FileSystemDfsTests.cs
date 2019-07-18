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

using System.IO;
using System.IO.Abstractions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Modules.Lib.Dfs;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit;
using IFileSystem = Catalyst.Common.Interfaces.FileSystem.IFileSystem;

namespace Catalyst.Modules.Lib.UnitTests.Dfs
{
    public class FileSystemDfsTests
    {
        private readonly IFileSystem _fileSystem;
        private readonly BLAKE2B_16 _hashingAlgorithm;

        public FileSystemDfsTests()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            var file = Substitute.For<IFile>();
            _fileSystem.File.Returns(file);
            _fileSystem.GetCatalystDataDir()
               .Returns(new DirectoryInfo("whatever"));
            _hashingAlgorithm = new BLAKE2B_16();
        }

        [Fact]
        public async Task AddTextAsync_Should_Assume_UTF8_Content()
        {
            var dfs = new FileSystemDfs(_hashingAlgorithm, _fileSystem);

            var someGoodUtf8Content = "some good utf8 content!";

            var contentHash = await dfs.AddTextAsync(someGoodUtf8Content);

            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Any<string>(), 
                Arg.Is<string>(b => b.Equals(someGoodUtf8Content)), 
                Arg.Is(Encoding.UTF8), 
                Arg.Any<CancellationToken>());

            var utf8Hash = someGoodUtf8Content.ComputeUtf8Multihash(_hashingAlgorithm);
            var uf32Hash = Encoding.UTF32.GetBytes(someGoodUtf8Content)
               .ComputeMultihash(_hashingAlgorithm);

            contentHash.Should().Be(utf8Hash.ToString());
            contentHash.Should().NotBe(uf32Hash.ToString());
        }
    }
}

