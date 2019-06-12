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
using System.Security.Cryptography.X509Certificates;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;
using DotNetty.Transport.Channels;
using IChannel = DotNetty.Transport.Channels.IChannel;

namespace Catalyst.Common.Interfaces.IO
{
    public interface ISocket : IDisposable
    {
        IChannel Channel { get; }
    }

    public interface IObservableSocket : ISocket
    {
        IObservable<IChanneledMessage<ProtocolMessage>> MessageStream { get; }
    }

    public interface IChannelFactory
    {
        IObservableSocket BuildChannel(IPAddress targetAddress = null,
            int targetPort = 0,
            X509Certificate2 certificate = null,
            IEventLoopGroup handlerEventLoopGroup = null);
    }
}
