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
using System.IO.Abstractions;
using System.Linq;
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
        private readonly IMultihashAlgorithm _hashingAlgorithm;
        private readonly FileSystemDfs _dfs;
        private readonly string _baseFolder;

        public FileSystemDfsTests()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            var file = Substitute.For<IFile>();
            _fileSystem.File.Returns(file);
            _fileSystem.GetCatalystDataDir()
               .Returns(new DirectoryInfo("correct-information"));
            _hashingAlgorithm = new BLAKE2B_32();
            _dfs = new FileSystemDfs(_hashingAlgorithm, _fileSystem);

            _baseFolder = Path.Combine(_fileSystem.GetCatalystDataDir().FullName,
                Common.Config.Constants.DfsDataSubDir);
        }

        [Fact]
        public async Task AddTextAsync_Should_Write_The_Correct_Content_as_UTF8()
        {
            var someGoodUtf8Content = "some good utf8 content!";

            var contentHash = await _dfs.AddTextAsync(someGoodUtf8Content);

            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Any<string>(), 
                Arg.Is<string>(b => b.Equals(someGoodUtf8Content)), 
                Arg.Is(Encoding.UTF8), 
                Arg.Any<CancellationToken>());

            var utf8Hash = Encoding.UTF8.GetBytes(someGoodUtf8Content)
               .ComputeMultihash(_hashingAlgorithm).AsBase64UrlString();
            var uf32Hash = Encoding.UTF32.GetBytes(someGoodUtf8Content)
               .ComputeMultihash(_hashingAlgorithm).AsBase64UrlString();

            contentHash.Should().Be(utf8Hash);
            contentHash.Should().NotBe(uf32Hash);
        }

        [Fact]
        public async Task AddTextAsync_Should_Save_File_In_Subfolder_With_Hash_As_Name()
        {
            var someGoodUtf8Content = "some good utf8 content!";

            var filename = await _dfs.AddTextAsync(someGoodUtf8Content);

            var expectedFileName = Encoding.UTF8.GetBytes(someGoodUtf8Content)
               .ComputeMultihash(_hashingAlgorithm).AsBase64UrlString();

            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Is(Path.Combine(_baseFolder, expectedFileName)),
                Arg.Any<string>(),
                Arg.Is(Encoding.UTF8),
                Arg.Any<CancellationToken>());

            filename.Should().Be(expectedFileName);
        }

        [Fact]
        public async Task AddTextAsync_Should_Be_Cancellable()
        {
            var cancellationToken = new CancellationToken();
            await _dfs.AddTextAsync("good morning", cancellationToken);
            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task ReadTextAsync_Should_Assume_UTF8_Content()
        {
            await _dfs.ReadTextAsync("hello");
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Any<string>(),
                Arg.Is(Encoding.UTF8),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReadTextAsync_Should_Point_To_The_Correct_File()
        {
            var filHash = "hello";
            await _dfs.ReadTextAsync(filHash);
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Is<string>(s => s.Equals(Path.Combine(_baseFolder, filHash))),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReadTextAsync_Be_Cancellable()
        {
            var cancellationToken = new CancellationToken();
            await _dfs.ReadTextAsync(
                @"https://media.giphy.com/media/KZwQMLTSx7M8bJ9OkZ/giphy.gif", 
                cancellationToken);
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task AddAsync_Should_Write_The_Correct_Content()
        {
            var fakeContent = "<:3)~~~~".ToMemoryStream();
            var expectedBytes = await fakeContent.ReadAllBytesAsync(CancellationToken.None);

            await _dfs.AddAsync(fakeContent);

            await _fileSystem.File.Received(1).WriteAllBytesAsync(
                Arg.Any<string>(),
                Arg.Is<byte[]>(b => b.SequenceEqual(expectedBytes)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AddAsync_Should_Save_File_In_Subfolder_With_Hash_As_Name()
        {
            var someContent = BitConverter.GetBytes(123456);
            var streamed = someContent.ToMemoryStream();
            var contentBytes = await streamed
               .ReadAllBytesAsync(CancellationToken.None);

            var expectedFileName = contentBytes
               .ComputeMultihash(_hashingAlgorithm).AsBase64UrlString();

            var filename = await _dfs.AddAsync(streamed);

            await _fileSystem.File.Received(1).WriteAllBytesAsync(
                Arg.Is(Path.Combine(_baseFolder, expectedFileName)),
                Arg.Any<byte[]>(),
                Arg.Any<CancellationToken>());

            filename.Should().Be(expectedFileName);
        }

        [Fact]
        public async Task AddAsync_Should_Be_Cancellable()
        {
            var cancellationToken = new CancellationToken();
            await _dfs.AddAsync("Hello there".ToMemoryStream(), 
                cancellationToken: cancellationToken);
            await _fileSystem.File.Received(1).WriteAllBytesAsync(
                Arg.Any<string>(),
                Arg.Any<byte[]>(),
                Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task ReadAsync_Should_Point_To_The_Correct_File()
        {
            var fileName = "myFileHash";
            await _dfs.ReadAsync(fileName);
            _fileSystem.File.Received(1)
               .OpenRead(Arg.Is<string>(s => s.Equals(Path.Combine(_baseFolder, fileName))));
        }

        [Fact]
        public async Task Constructor_Should_Throw_On_Hash_Default_Size_Above_191()
        {
            var tooLong = new SKEIN1024_192();
            new Action(() => new FileSystemDfs(tooLong, _fileSystem)).Should().Throw<ArgumentException>()
               .And.Message.Should().Contain(nameof(SKEIN1024_192));

            var longEnough = new SKEIN1024_184();
            new Action(() => new FileSystemDfs(longEnough, _fileSystem)).Should().NotThrow<ArgumentException>();
        }
    }
}

