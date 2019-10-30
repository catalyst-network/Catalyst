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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Hashing;
using FluentAssertions;
using LibP2P;
using NSubstitute;
using Serilog;
using TheDotNetLeague.Ipfs.Abstractions;
using TheDotNetLeague.Ipfs.Core.Lib;
using TheDotNetLeague.Ipfs.Core.Lib.CoreApi;
using TheDotNetLeague.MultiFormats.MultiHash;
using Xunit;

namespace Catalyst.Core.Modules.Dfs.Tests.UnitTests
{
    public sealed class DfsTests : IDisposable
    {
        private const int DelayInMs = 300;
        private readonly ICoreApi _ipfsEngine;
        private readonly Cid _expectedCid;
        private readonly IFileSystemNode _addedRecord;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Dfs _dfs;
        private readonly HashProvider _hashProvider;

        public DfsTests()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));

            _ipfsEngine = Substitute.For<ICoreApi>();
            var fileSystem = Substitute.For<IFileSystemApi>();
            _ipfsEngine.FileSystem.Returns(fileSystem);

            var logger = Substitute.For<ILogger>();
            _expectedCid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("data"));

            _addedRecord = Substitute.For<IFileSystemNode>();
            _addedRecord.Id.ReturnsForAnyArgs(_expectedCid);
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(DelayInMs));

            _dfs = new Dfs(_ipfsEngine, _hashProvider, logger);
        }

        [Fact]
        public async Task AddTextAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        {
            _ipfsEngine.FileSystem.AddTextAsync("good morning", Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var record = await _dfs.AddTextAsync("good morning");
            record.Should().Be(_expectedCid);
        }

        [Fact]
        public async Task AddAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        {
            _ipfsEngine.FileSystem.AddAsync(Stream.Null, Arg.Any<string>(), Arg.Any<AddFileOptions>(),
                    Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var record = await _dfs.AddAsync(Stream.Null);
            record.Should().Be(_expectedCid);
        }

        [Fact]
        public async Task ReadAsync_Should_Rely_On_IpfsEngine_And_Return_Streamed_Content()
        {
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("file"));
            _ipfsEngine.FileSystem
               .ReadFileAsync(cid.Encode(), Arg.Any<CancellationToken>())
               .Returns(c => "the content".ToMemoryStream());

            using (var stream = await _dfs.ReadAsync(cid.Encode()))
            {
                stream.ReadAllAsUtf8String(false).Should().Be("the content");
            }
        }

        [Fact]
        public async Task ReadTextAsync_Should_Rely_On_IpfsEngine_And_Return_Text_Content()
        {
            var cid = CidHelper.CreateCid(_hashProvider.ComputeUtf8MultiHash("file"));
            _ipfsEngine.FileSystem
               .ReadAllTextAsync(cid.Encode(), Arg.Any<CancellationToken>())
               .Returns(c => "the other content");

            var text = await _dfs.ReadTextAsync(cid.Encode());
            text.Should().Be("the other content");
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _cancellationTokenSource?.Dispose();
        }

        public void Dispose() { Dispose(true); }
    }
}
