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
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using FluentAssertions;
using MultiFormats.Registry;
using NSubstitute;
using Xunit;
using IFileSystem = Catalyst.Abstractions.FileSystem.IFileSystem;

namespace Catalyst.Core.Modules.Dfs.Tests.UnitTests
{
    public class DevDfsTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IFileSystem _fileSystem;
        private readonly DevDfs _dfs;
        private readonly string _baseFolder;

        public DevDfsTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
            _fileSystem = Substitute.For<IFileSystem>();
            var file = Substitute.For<IFile>();
            _fileSystem.File.Returns(file);
            _fileSystem.GetCatalystDataDir()
               .Returns(new DirectoryInfo("correct-information"));
            _dfs = new DevDfs(_fileSystem, _hashProvider);

            _baseFolder = Path.Combine(_fileSystem.GetCatalystDataDir().FullName,
                Constants.DfsDataSubDir);
        }

        [Fact]
        public async Task AddAsync_Should_Be_Cancellable()
        {
            _fileSystem.File.Create(Arg.Any<string>()).Returns(new MemoryStream());
            var contentStream = Substitute.For<Stream>();
            var cancellationToken = new CancellationToken();
            await _dfs.FileSystem.AddAsync(contentStream, cancel: cancellationToken);

            await contentStream.Received(1).CopyToAsync(Arg.Any<Stream>(), Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task AddAsync_Should_Save_File_In_Subfolder_With_Hash_As_Name()
        {
            _fileSystem.File.Create(Arg.Any<string>()).Returns(new MemoryStream());

            var contentBytes = BitConverter.GetBytes(123456);
            var contentStream = contentBytes.ToMemoryStream();

            var expectedCid = CidHelper.CreateCid(_hashProvider.ComputeMultiHash(contentBytes));

            var cid = await _dfs.FileSystem.AddAsync(contentStream);

            cid.Should().Be(expectedCid);
        }

        [Fact]
        public async Task AddAsync_Should_Write_The_Correct_Content()
        {
            var content = "<:3)~~~~";
            var contentStream = content.ToMemoryStream();

            var resultStream = new MemoryStream();

            var mockFileStream = Substitute.For<Stream>();
            mockFileStream.CanWrite.Returns(true);
            mockFileStream.When(m => m.WriteAsync(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>())).Do(x =>
                resultStream.Write(x.ArgAt<byte[]>(0), x.ArgAt<int>(1), x.ArgAt<int>(2)));

            _fileSystem.File.Create(Arg.Any<string>()).Returns(mockFileStream);

            await _dfs.FileSystem.AddAsync(contentStream);

            var resultBytes = await resultStream.ReadAllBytesAsync(CancellationToken.None);
            var resultContent = Encoding.UTF8.GetString(resultBytes);
            resultContent.Should().Be(content);
        }

        [Fact]
        public async Task AddTextAsync_Should_Be_Cancellable()
        {
            var cancellationToken = new CancellationToken();
            await _dfs.FileSystem.AddTextAsync("good morning", cancel: cancellationToken);
            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task AddTextAsync_Should_Save_File_In_Subfolder_With_Hash_As_Name()
        {
            var someGoodUtf8Content = "some good utf8 content!";

            var cid = await _dfs.FileSystem.AddTextAsync(someGoodUtf8Content);
            var expectedCid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash(someGoodUtf8Content));

            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Is(Path.Combine(_baseFolder, expectedCid.Encode())),
                Arg.Any<string>(),
                Arg.Is(Encoding.UTF8),
                Arg.Any<CancellationToken>());

            cid.Should().Be(expectedCid);
        }

        [Fact]
        public async Task AddTextAsync_Should_Write_The_Correct_Content_as_UTF8()
        {
            var someGoodUtf8Content = "some good utf8 content!";

            var cid = await _dfs.FileSystem.AddTextAsync(someGoodUtf8Content);

            await _fileSystem.File.Received(1).WriteAllTextAsync(
                Arg.Any<string>(),
                Arg.Is<string>(b => b.Equals(someGoodUtf8Content)),
                Arg.Is(Encoding.UTF8),
                Arg.Any<CancellationToken>());

            var utf8Hash = _hashProvider.ComputeUtf8MultiHash(someGoodUtf8Content);
            var uf32Hash = _hashProvider.ComputeMultiHash(Encoding.UTF32.GetBytes(someGoodUtf8Content));

            cid.Id.Hash.Should().Be(utf8Hash);
            cid.Id.Hash.Should().NotBe(uf32Hash);
        }

        [Fact]
        public void Constructor_Should_Throw_On_Hash_Default_Size_Above_159()
        {
            HashingAlgorithm.Register("TooLong", 0x9999, 160);

            var toLongHashingAlgorithm = HashingAlgorithm.All.First(x => x.DigestSize > 159);
            var longEnoughHashingAlgorithm = HashingAlgorithm.All.First(x => x.DigestSize <= 159);

            // ReSharper disable once ObjectCreationAsStatement
            new Action(() => new DevDfs(_fileSystem, new HashProvider(toLongHashingAlgorithm))).Should()
               .Throw<ArgumentException>()
               .And.Message.Should().Contain(nameof(HashingAlgorithm));

            // ReSharper disable once ObjectCreationAsStatement
            new Action(() => new DevDfs(_fileSystem, new HashProvider(longEnoughHashingAlgorithm))).Should()
               .NotThrow<ArgumentException>();
        }

        [Fact]
        public async Task ReadAsync_Should_Point_To_The_Correct_File()
        {
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("file"));
            await _dfs.FileSystem.ReadFileAsync(cid);
            _fileSystem.File.Received(1)
               .OpenRead(Arg.Is<string>(s => s.Equals(Path.Combine(_baseFolder, cid.Encode()))));
        }

        [Fact]
        public async Task ReadTextAsync_Be_Cancellable()
        {
            var cancellationToken = new CancellationToken();
            await _dfs.FileSystem.ReadAllTextAsync(
                _hashProvider.ComputeUtf8MultiHash(@"https://media.giphy.com/media/KZwQMLTSx7M8bJ9OkZ/giphy.gif").ToString(),
                cancellationToken);
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Any<string>(),
                Arg.Any<Encoding>(),
                Arg.Is(cancellationToken));
        }

        [Fact]
        public async Task ReadTextAsync_Should_Assume_UTF8_Content()
        {
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("file"));
            await _dfs.FileSystem.ReadAllTextAsync(cid);
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Any<string>(),
                Arg.Is(Encoding.UTF8),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReadTextAsync_Should_Point_To_The_Correct_File()
        {
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("file"));
            await _dfs.FileSystem.ReadAllTextAsync(cid);
            await _fileSystem.File.Received(1).ReadAllTextAsync(
                Arg.Is<string>(s => s.Equals(Path.Combine(_baseFolder, cid.Encode()))),
                Arg.Any<Encoding>(),
                Arg.Any<CancellationToken>());
        }
    }
}
