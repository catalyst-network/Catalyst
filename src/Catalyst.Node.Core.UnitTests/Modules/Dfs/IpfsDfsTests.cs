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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Modules.Dfs;
using Catalyst.Node.Core.Modules.Dfs;
using FluentAssertions;
using Ipfs;
using Ipfs.CoreApi;
using Serilog;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTest.Modules.Dfs
{
    public sealed class IpfsDfsTests : IDisposable
    {
        private const int DelayInMs = 50;
        private const int DelayMultiplier = 4;
        private readonly IIpfsEngine _ipfsEngine;
        private readonly ILogger _logger;
        private readonly Cid _expectedCid;
        private readonly IFileSystemNode _addedRecord;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public IpfsDfsTests()
        {
            _ipfsEngine = Substitute.For<IIpfsEngine>();
            var fileSystem = Substitute.For<IFileSystemApi>();
            _ipfsEngine.FileSystem.Returns(fileSystem);

            _logger = Substitute.For<ILogger>();
            var hashBits = Guid.NewGuid().ToByteArray().Concat(new byte[16]).ToArray();
            _expectedCid = new Cid
            {
                Encoding = "base64",
                Hash = new MultiHash(IpfsDfs.HashAlgorithm, hashBits)
            };

            _addedRecord = Substitute.For<IFileSystemNode>();
            _addedRecord.Id.ReturnsForAnyArgs(_expectedCid);
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(DelayInMs));
        }

        [Fact]
        public async Task AddTextAsync_should_rely_on_ipfsEngine_and_return_record_id()
        {
            _ipfsEngine.FileSystem.AddTextAsync("good morning", Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            var record = await dfs.AddTextAsync("good morning");
            Cid.Decode(record).Should().Be(_expectedCid);
        }

        [Fact]
        public async Task AddAsync_should_rely_on_ipfsEngine_and_return_record_id()
        {
            _ipfsEngine.FileSystem.AddAsync(Stream.Null, Arg.Any<string>(), Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            var record = await dfs.AddAsync(Stream.Null);
            Cid.Decode(record).Should().Be(_expectedCid);
        }

        [Fact]
        public async Task ReadAsync_should_rely_on_ipfsEngine_and_return_streamed_content()
        {
            _ipfsEngine.FileSystem
               .ReadFileAsync("some path", Arg.Any<CancellationToken>())
               .Returns(c => "the content".ToMemoryStream());

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            using (var stream = await dfs.ReadAsync("some path"))
            {
                stream.ReadAllAsUtf8String(false).Should().Be("the content");
            }
        }

        [Fact]
        public async Task ReadTextAsync_should_rely_on_ipfsEngine_and_return_text_content()
        {
            _ipfsEngine.FileSystem
               .ReadAllTextAsync("some path", Arg.Any<CancellationToken>())
               .Returns(c => "the other content");

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            var text = await dfs.ReadTextAsync("some path");
            text.Should().Be("the other content");
        }

        [Fact]
        public void AddTextAsync_should_be_cancellable()
        {
            _ipfsEngine.FileSystem.AddTextAsync(Arg.Any<string>(), Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c =>
                {
                    Task.Delay(DelayInMs * DelayMultiplier, (CancellationToken) c[2]).GetAwaiter().GetResult();
                    return Task.FromResult(_addedRecord);
                });

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            new Action(() => dfs.AddTextAsync("this is taking too long", _cancellationTokenSource.Token)
                   .GetAwaiter().GetResult()).Should().Throw<TaskCanceledException>()
               .And.CancellationToken.Should().Be(_cancellationTokenSource.Token);
        }

        [Fact]
        public void AddAsync_should_be_cancellable()
        {
            _ipfsEngine.FileSystem.AddAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c =>
                {
                    Task.Delay(DelayInMs * DelayMultiplier, (CancellationToken) c[3]).GetAwaiter().GetResult();
                    return Task.FromResult(_addedRecord);
                });

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            new Action(() => dfs.AddAsync(Stream.Null, "this is taking too long", _cancellationTokenSource.Token)
                   .GetAwaiter().GetResult()).Should().Throw<TaskCanceledException>()
               .And.CancellationToken.Should().Be(_cancellationTokenSource.Token);
        }

        [Fact]
        public void ReadTextAsync_should_be_cancellable()
        {
            _ipfsEngine.FileSystem.ReadAllTextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(c =>
                {
                    Task.Delay(DelayInMs * DelayMultiplier, (CancellationToken) c[1]).GetAwaiter().GetResult();
                    return Task.FromResult("some content");
                });

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            new Action(() => dfs.ReadTextAsync("path", _cancellationTokenSource.Token)
                   .GetAwaiter().GetResult()).Should().Throw<TaskCanceledException>()
               .And.CancellationToken.Should().Be(_cancellationTokenSource.Token);
        }

        [Fact]
        public void ReadAsync_should_be_cancellable()
        {
            _ipfsEngine.FileSystem.ReadFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(c =>
                {
                    Task.Delay(DelayInMs * DelayMultiplier, (CancellationToken) c[1]).GetAwaiter().GetResult();
                    return Task.FromResult(Stream.Null);
                });

            var dfs = new IpfsDfs(_ipfsEngine, _logger);
            new Action(() => dfs.ReadAsync("path", _cancellationTokenSource.Token)
                   .GetAwaiter().GetResult()).Should().Throw<TaskCanceledException>()
               .And.CancellationToken.Should().Be(_cancellationTokenSource.Token);
        }

        public void Dispose()
        {
            _ipfsEngine?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
