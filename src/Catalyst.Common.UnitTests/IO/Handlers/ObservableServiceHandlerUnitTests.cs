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
using Catalyst.Common.IO.Handlers;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Handlers
{
    public sealed class ObservableServiceHandlerUnitTests
    {
        public ObservableServiceHandlerUnitTests()
        {
            var testScheduler = new TestScheduler();
            _observableServiceHandler = new ObservableServiceHandler(testScheduler);
        }

        private readonly ObservableServiceHandler _observableServiceHandler;

        [Fact]
        public void Dispose_Should_Dispose_ObservableServiceHandler() { _observableServiceHandler.Dispose(); }

        [Fact]
        public void MessageStream_Should_Call_OnError_On_ExceptionCaught()
        {
            var channelHandlerContext = Substitute.For<IChannelHandlerContext>();
            var exception = new Exception("X.X");

            _observableServiceHandler.MessageStream.Subscribe(
                nextResponse => { },
                errorResponse => { exception.Should().Be(errorResponse); });

            _observableServiceHandler.ExceptionCaught(channelHandlerContext, exception);
        }
    }
}
