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
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Catalyst.Core.IO.Transport.Channels;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport.Channels
{
    public sealed class ClientChannelInitializerBaseTests
    {
        public ClientChannelInitializerBaseTests()
        {
            var handlerGenerationFunction = Substitute.For<Func<IList<IChannelHandler>>>();
            _clientChannelInitializerBase =
                new ClientChannelInitializerBase<IChannel>(handlerGenerationFunction, null);
        }

        private readonly ClientChannelInitializerBase<IChannel> _clientChannelInitializerBase;

        [Fact]
        public void NewTlsHandler_With_Correct_Parameters_Should_Return_TlsHandler()
        {
            var tlsHandler =
                _clientChannelInitializerBase.NewTlsHandler(IPAddress.Any, Substitute.For<X509Certificate>());
            tlsHandler.Should().BeOfType<TlsHandler>();
        }

        [Fact]
        public void NewTlsHandler_With_Null_Parameters_Should_Return_Null()
        {
            var tlsHandler = _clientChannelInitializerBase.NewTlsHandler(null, null);
            tlsHandler.Should().BeNull();
        }

        [Fact]
        public void NewTlsHandler_With_Null_TargetHost_Should_Return_Null()
        {
            var tlsHandler = _clientChannelInitializerBase.NewTlsHandler(null, Substitute.For<X509Certificate>());
            tlsHandler.Should().BeNull();
        }

        [Fact]
        public void NewTlsHandler_With_Null_X509Certificate_Should_Return_Null()
        {
            var tlsHandler = _clientChannelInitializerBase.NewTlsHandler(IPAddress.Any, null);
            tlsHandler.Should().BeNull();
        }

        [Fact]
        public void When_ToString_Is_Called_Return_Custom_String()
        {
            const string targetValue = "OutboundInitializer[IChannel]";
            var stringValue = _clientChannelInitializerBase.ToString();
            stringValue.Should().Be(targetValue);
        }
    }
}
