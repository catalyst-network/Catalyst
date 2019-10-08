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
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Hashing;
using FluentAssertions;
using Ipfs;
using Ipfs.CoreApi;
using Ipfs.Registry;
using NSubstitute;
using Serilog;
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
            var hashBits = CorrelationId.GenerateCorrelationId().Id.ToByteArray().Concat(new byte[16]).ToArray();
            _expectedCid = new Cid
            {
                Encoding = _hashProvider.HashingAlgorithm.ToString().ToLowerInvariant(),
                Hash = _hashProvider.Cast(hashBits)
            };

            _addedRecord = Substitute.For<IFileSystemNode>();
            _addedRecord.Id.ReturnsForAnyArgs(_expectedCid);
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(DelayInMs));

            _dfs = new Dfs(_ipfsEngine, _hashProvider, logger);
        }

        //[Fact]
        //public async Task AddTextAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        //{
        //    _ipfsEngine.FileSystem.AddTextAsync("good morning", Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
        //       .Returns(c => Task.FromResult(_addedRecord));

        //    var record = await _dfs.AddTextAsync("good morning");
        //    Cid.Decode(record).Should().Be(_expectedCid);
        //}

        //[Fact]
        //public async Task AddAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        //{
        //    _ipfsEngine.FileSystem.AddAsync(Stream.Null, Arg.Any<string>(), Arg.Any<AddFileOptions>(),
        //            Arg.Any<CancellationToken>())
        //       .Returns(c => Task.FromResult(_addedRecord));

        //    var record = await _dfs.AddAsync(Stream.Null);
        //    Cid.Decode(record).Should().Be(_expectedCid);
        //}

        [Fact]
        public async Task ReadAsync_Should_Rely_On_IpfsEngine_And_Return_Streamed_Content()
        {
            _ipfsEngine.FileSystem
               .ReadFileAsync("some path", Arg.Any<CancellationToken>())
               .Returns(c => "the content".ToMemoryStream());

            using (var stream = await _dfs.ReadAsync("some path"))
            {
                stream.ReadAllAsUtf8String(false).Should().Be("the content");
            }
        }

        [Fact]
        public async Task ReadTextAsync_Should_Rely_On_IpfsEngine_And_Return_Text_Content()
        {
            _ipfsEngine.FileSystem
               .ReadAllTextAsync("some path", Arg.Any<CancellationToken>())
               .Returns(c => "the other content");

            var text = await _dfs.ReadTextAsync("some path");
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
