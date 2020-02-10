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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Dfs;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Hashing;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats.Registry;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests.Deltas
{
    public sealed class DeltaDfsReaderTests
    {
        private readonly IHashProvider _hashProvider;
        private readonly IDfsService _dfsService;
        private readonly ILogger _logger;
        private readonly IDeltaDfsReader _dfsReader;

        public DeltaDfsReaderTests()
        {
            var hashingAlgorithm = HashingAlgorithm.GetAlgorithmMetadata("blake2b-256");
            _hashProvider = new HashProvider(hashingAlgorithm);
            _dfsService = Substitute.For<IDfsService>();
            _logger = Substitute.For<ILogger>();

            _dfsReader = new DeltaDfsReader(_dfsService, _logger);
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Return_False_And_Log_When_Hash_Not_Found_On_Dfs()
        {
            var exception = new FileNotFoundException("that hash is not good");
            _dfsService.UnixFsApi.ReadFileAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Throws(exception);

            var cid = _hashProvider.ComputeUtf8MultiHash("bad hash").ToCid();
            _dfsReader.TryReadDeltaFromDfs(cid, out _, CancellationToken.None).Should().BeFalse();
            _logger.Received(1).Error(exception,
                Arg.Any<string>(),
                Arg.Is<Cid>(s => s == cid));
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Return_True_When_Hash_Found_On_Dfs_And_Delta_Is_Valid()
        {
            var cid = _hashProvider.ComputeUtf8MultiHash("good hash").ToCid().ToString();
            var matchingDelta = DeltaHelper.GetDelta(_hashProvider);

            _dfsService.UnixFsApi.ReadFileAsync(cid, CancellationToken.None)
               .Returns(matchingDelta.ToByteArray().ToMemoryStream());

            var found = _dfsReader.TryReadDeltaFromDfs(cid, out var delta, CancellationToken.None);

            found.Should().BeTrue();
            delta.Should().Be(matchingDelta);
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Return_False_When_Hash_Found_On_Dfs_And_Delta_Is_Not_Valid()
        {
            var cid = _hashProvider.ComputeUtf8MultiHash("good hash").ToCid();
            var matchingDelta = DeltaHelper.GetDelta(_hashProvider);
            matchingDelta.PreviousDeltaDfsHash = ByteString.Empty;

            new Action(() => matchingDelta.IsValid()).Should()
               .Throw<InvalidDataException>("otherwise this test is useless");

            _dfsService.UnixFsApi.ReadFileAsync(cid, CancellationToken.None)
               .Returns(matchingDelta.ToByteArray().ToMemoryStream());

            var found = _dfsReader.TryReadDeltaFromDfs(cid, out var delta, CancellationToken.None);

            found.Should().BeFalse();
            delta.Should().BeNull();
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Pass_Cancellation_Token()
        {
            var cid = _hashProvider.ComputeUtf8MultiHash("good hash").ToCid();
            var cancellationToken = new CancellationToken();

            var matchingDelta = DeltaHelper.GetDelta(_hashProvider);
            _dfsService.UnixFsApi.ReadFileAsync(cid, CancellationToken.None)
               .Returns(matchingDelta.ToByteArray().ToMemoryStream());

            _dfsReader.TryReadDeltaFromDfs(cid, out _, CancellationToken.None);

            _dfsService.UnixFsApi.Received(1)?.ReadFileAsync(Arg.Is(cid.ToString()), Arg.Is(cancellationToken));
        }
    }
}

