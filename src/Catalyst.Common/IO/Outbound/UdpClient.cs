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
using System.Net;
using Catalyst.Common.Interfaces.IO;
using Catalyst.Common.Interfaces.IO.Outbound;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Common.IO.Outbound
{
    public class UdpClient : ClientBase, IUdpClient
    {
        protected UdpClient(IUdpChannelFactory channelFactory, ILogger logger) 
            : base(channelFactory, logger) { }

        protected sealed override void Bootstrap(IChannelHandler channelHandler, IPEndPoint ipEndPoint)
        {
            Channel = new Bootstrap()
               .Group(WorkerEventLoop)
               .ChannelFactory(ChannelFactory.BuildChannel)
               .Option(ChannelOption.SoBroadcast, true)
               .Handler(new LoggingHandler(LogLevel.DEBUG))
               .Handler(channelHandler)
               .BindAsync(ipEndPoint.Address, IPEndPoint.MinPort)
               .GetAwaiter()
               .GetResult();
        }
    }
}

