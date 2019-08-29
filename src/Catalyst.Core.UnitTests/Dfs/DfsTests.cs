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
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Config;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.TestUtils;
using FluentAssertions;
using Ipfs;
using Ipfs.CoreApi;
using NSubstitute;
using NSubstitute.Core;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Dfs
{
    public sealed class DfsTests : IDisposable
    {
        private const int DelayInMs = 300;
        private const int DelayMultiplier = 6;
        private readonly IIpfsAdapter _ipfsEngine;
        private readonly Cid _expectedCid;
        private readonly IFileSystemNode _addedRecord;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Core.Dfs.Dfs _dfs;

        public DfsTests()
        {
            _ipfsEngine = Substitute.For<IIpfsAdapter>();
            var fileSystem = Substitute.For<IFileSystemApi>();
            _ipfsEngine.FileSystem.Returns(fileSystem);

            var logger = Substitute.For<ILogger>();
            var hashBits = CorrelationId.GenerateCorrelationId().Id.ToByteArray().Concat(new byte[16]).ToArray();
            _expectedCid = new Cid
            {
                Encoding = Constants.EncodingAlgorithm.ToString().ToLowerInvariant(),
                Hash = new MultiHash(MultiHash.GetHashAlgorithmName(Constants.HashAlgorithmType.GetHashCode()), hashBits)
            };

            _addedRecord = Substitute.For<IFileSystemNode>();
            _addedRecord.Id.ReturnsForAnyArgs(_expectedCid);
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(DelayInMs));

            _dfs = new Core.Dfs.Dfs(_ipfsEngine, logger);
        }

        [Fact]
        public async Task AddTextAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        {
            _ipfsEngine.FileSystem.AddTextAsync("good morning", Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var record = await _dfs.AddTextAsync("good morning");
            Cid.Decode(record).Should().Be(_expectedCid);
        }

        [Fact]
        public async Task AddAsync_Should_Rely_On_IpfsEngine_And_Return_Record_Id()
        {
            _ipfsEngine.FileSystem.AddAsync(Stream.Null, Arg.Any<string>(), Arg.Any<AddFileOptions>(), Arg.Any<CancellationToken>())
               .Returns(c => Task.FromResult(_addedRecord));

            var record = await _dfs.AddAsync(Stream.Null);
            Cid.Decode(record).Should().Be(_expectedCid);
        }

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

        [Fact]
        public void AddTextAsync_Should_Be_Cancellable()
        {
            _ipfsEngine.FileSystem.AddTextAsync(default, default, default)
               .ReturnsForAnyArgs(ci => SimulateTaskLongerThanCancellation(ci, 2, _addedRecord));

            AssertAsyncCallGotCancelled(_dfs.AddTextAsync("this is taking too long", _cancellationTokenSource.Token));
        }

        [Fact]
        public void AddAsync_Should_Be_Cancellable()
        {
            _ipfsEngine.FileSystem.AddAsync(default, default, default, default)
               .ReturnsForAnyArgs(ci => SimulateTaskLongerThanCancellation(ci, 3, _addedRecord));

            AssertAsyncCallGotCancelled(_dfs.AddAsync(Stream.Null, "this is taking too long", _cancellationTokenSource.Token));
        }

        [Fact]
        public void ReadTextAsync_Should_Be_Cancellable()
        {
            _ipfsEngine.FileSystem.ReadAllTextAsync(default, default)
               .ReturnsForAnyArgs(ci => SimulateTaskLongerThanCancellation(ci, 1, "some result"));

            AssertAsyncCallGotCancelled(_dfs.ReadTextAsync("path", _cancellationTokenSource.Token));
        }

        [Fact]
        public void ReadAsync_Should_Be_Cancellable()
        {
            _ipfsEngine.FileSystem.ReadFileAsync(default, default)
               .ReturnsForAnyArgs(ci => SimulateTaskLongerThanCancellation(ci, 1, Stream.Null));

            AssertAsyncCallGotCancelled(_dfs.ReadAsync("path", _cancellationTokenSource.Token));
        }

        private void AssertAsyncCallGotCancelled<T>(Task<T> asyncCall)
        {
            new Action(() => asyncCall.GetAwaiter().GetResult()).Should().Throw<TaskCanceledException>()
               .And.CancellationToken.Should().Be(_cancellationTokenSource.Token);
        }

        private async Task<T> SimulateTaskLongerThanCancellation<T>(CallInfo callInfo, int cancellationTokenArgPosition, T returnValue)
        {
            var cancellationToken = (CancellationToken) callInfo[cancellationTokenArgPosition];
            await TaskHelper.WaitForAsync(() => cancellationToken.IsCancellationRequested,
                TimeSpan.FromMilliseconds(DelayInMs * DelayMultiplier)).ConfigureAwait(false);
            await Task.Delay(DelayInMs, cancellationToken).ConfigureAwait(false);
            return returnValue;
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
