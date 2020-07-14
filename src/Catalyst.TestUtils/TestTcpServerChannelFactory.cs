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
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Catalyst.Abstractions.IO.Handlers;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.EventLoop;
using Catalyst.Modules.Network.Dotnetty.Abstractions.IO.Transport.Channels;
using Catalyst.Modules.Network.Dotnetty.IO.Handlers;
using Catalyst.Modules.Network.Dotnetty.IO.Transport.Channels;
using DotNetty.Transport.Channels;
using MultiFormats;
using NSubstitute;

namespace Catalyst.TestUtils
{
    public class TestTcpServerChannelFactory : TcpServerChannelFactory
    {
        private readonly IObservableServiceHandler _observableServiceHandler;

        public TestTcpServerChannelFactory(int backLogValue = 100) : base(backLogValue)
        {
            _observableServiceHandler = new ObservableServiceHandler();
        }

        protected override Func<List<IChannelHandler>> HandlerGenerationFunction
        {
            get
            {
                return () => new List<IChannelHandler>
                {
                    _observableServiceHandler
                };
            }
        }

        /// <param name="eventLoopGroupFactory"></param>
        /// <param name="targetAddress">Ignored</param>
        /// <param name="targetPort">Ignored</param>
        /// <param name="certificate">Local TLS certificate</param>
        public override async Task<IObservableChannel> BuildChannelAsync(IEventLoopGroupFactory eventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate = null)
        {
            var channel = Substitute.For<IChannel>();

            var messageStream = _observableServiceHandler.MessageStream;

            return await Task.FromResult(new ObservableChannel(messageStream, channel)).ConfigureAwait(false);
        }

        public new Task<IChannel> BootstrapAsync(IEventLoopGroupFactory handlerEventLoopGroupFactory,
            MultiAddress address,
            X509Certificate2 certificate)
        {
            return base.BootstrapAsync(handlerEventLoopGroupFactory, address, certificate);
        }
    }
}
