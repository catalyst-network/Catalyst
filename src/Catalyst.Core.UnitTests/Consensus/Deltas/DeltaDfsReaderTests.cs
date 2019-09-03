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
using System.Threading;
using Catalyst.Abstractions.Dfs;
using Catalyst.Core.Consensus.Deltas;
using Catalyst.Core.Extensions;
using Catalyst.TestUtils;
using FluentAssertions;
using Google.Protobuf;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using Xunit;

namespace Catalyst.Core.UnitTests.Consensus.Deltas
{
    public class DeltaDfsReaderTests
    {
        private readonly IDfs _dfs;
        private readonly ILogger _logger;
        private readonly DeltaDfsReader _dfsReader;

        public DeltaDfsReaderTests()
        {
            _dfs = Substitute.For<IDfs>();
            _logger = Substitute.For<ILogger>();

            _dfsReader = new DeltaDfsReader(_dfs, _logger);
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Return_False_And_Log_When_Hash_Not_Found_On_Dfs()
        {
            var exception = new FileNotFoundException("that hash is not good");
            _dfs.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Throws(exception);

            var badHash = "bad hash";
            _dfsReader.TryReadDeltaFromDfs(badHash, out _, CancellationToken.None).Should().BeFalse();
            _logger.Received(1).Error(exception,
                Arg.Any<string>(),
                Arg.Is<string>(s => s == badHash));
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Return_True_When_Hash_Found_On_Dfs()
        {
            var goodHash = "good hash";
            var matchingDelta = DeltaHelper.GetDelta();

            _dfs.ReadAsync(goodHash, CancellationToken.None)
               .Returns(matchingDelta.ToByteArray().ToMemoryStream());

            var found = _dfsReader.TryReadDeltaFromDfs(goodHash, out var delta, CancellationToken.None);

            found.Should().BeTrue();
            delta.Should().Be(matchingDelta);
        }

        [Fact]
        public void TryReadDeltaFromDfs_Should_Pass_Cancellation_Token()
        {
            var goodHash = "good hash";
            var cancellationToken = new CancellationToken();

            var matchingDelta = DeltaHelper.GetDelta();
            _dfs.ReadAsync(goodHash, CancellationToken.None)
               .Returns(matchingDelta.ToByteArray().ToMemoryStream());

            _dfsReader.TryReadDeltaFromDfs(goodHash, out var delta, CancellationToken.None);

            _dfs.Received(1).ReadAsync(Arg.Is(goodHash), Arg.Is(cancellationToken));
        }
    }
}

