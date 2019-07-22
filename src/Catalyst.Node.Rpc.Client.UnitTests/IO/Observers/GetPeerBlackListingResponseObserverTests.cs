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
using System.Threading.Tasks;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Rpc.Client.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.IO.Observers
{
    /// <summary>
    /// Tests the CLI for peer blacklisting response
    /// </summary>
    public sealed class GetPeerBlackListingResponseObserverTests : IDisposable
    {
        private readonly IChannelHandlerContext _fakeContext;

        private readonly ILogger _logger;
        private PeerBlackListingResponseObserver _observer;

        /// <summary>
        /// Initializes a new instance of the <see>
        ///     <cref>GetPeerBlackListingResponseObserverTest</cref>
        /// </see>
        /// class. </summary>
        public GetPeerBlackListingResponseObserverTests()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}
