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

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Modules.Network.Dotnetty.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.IO.Transport.Channels;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using MultiFormats;
using NSubstitute;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.IO.Transport.Channels
{
    public sealed class TcpServerChannelFactoryTests
    {
        public TcpServerChannelFactoryTests()
        {
            var eventLoopGroupFactoryConfiguration = new EventLoopGroupFactoryConfiguration
            {
                TcpServerHandlerWorkerThreads = 2
            };

            _eventLoopGroupFactory = new TcpServerEventLoopGroupFactory(eventLoopGroupFactoryConfiguration);
            _address = "/ip4/127.0.0.1/tcp/9000";

            _testTcServerChannelFactory = new TestTcpServerChannelFactory();
        }

        private readonly TestTcpServerChannelFactory _testTcServerChannelFactory;
        private readonly TcpServerEventLoopGroupFactory _eventLoopGroupFactory;
        private readonly MultiAddress _address;

        [Test]
        public async Task Bootstrap_Should_Return_Channel()
        {
            var certificate = Substitute.For<X509Certificate2>();
            var channel =
                await _testTcServerChannelFactory.BootstrapAsync(_eventLoopGroupFactory, _address, certificate);

            channel.Should().BeAssignableTo<IChannel>();
        }

        [Test]
        public async Task BuildChannel_Should_Return_IObservableChannel()
        {
            var observableChannel =
                await _testTcServerChannelFactory.BuildChannelAsync(_eventLoopGroupFactory, _address).ConfigureAwait(false);

            observableChannel.Should().BeOfType<ObservableChannel>();
        }
    }
}
