/*
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

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO
{
    public abstract class AbstractSession
    {
        private IChannel Channel;
        protected ILogger _logger;

        public abstract Task OnConnected(IPeer peer, IChannel channel);

        public Task OnDisconnectNotice(object message,
            EndPoint channelEndPoint)
        {
            _logger.Debug("received disconnection message : {0}", message);
            _logger.Warning("channel {0} disconnected", channelEndPoint);
            return Task.CompletedTask;
        }
        
        public async Task OnError(Exception exception)
        {
            if (exception is DecoderException)
            {
                _logger.Warning($"Encountered an issue during encoding: {exception}. shutting down...");
                await Channel.CloseAsync();
                return;
            }

            if (exception is SocketException)
            {
                _logger.Warning($"Encountered an issue on the channel: {exception}. shutting down...");
                await Channel.CloseAsync();
                return;
            }

            _logger.Error($"Encountered an issue : {exception}");
        }

        public abstract void OnEventReceived(object message);
    }
}
