/**
 * (C) Copyright 2019 Catalyst-Network
 *
 * Author USER ${USER}$
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; version 2
 * of the License.
 */

using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Common.Helpers.IO.Outbound
{
    public abstract class AbstractClient : ISocketClient
    {
        public IChannel Channel { get; set; }
        
        public IBootstrap Client { get; set; }
        
        public abstract ISocketClient Bootstrap(IChannelHandler channelInitializer);

        public async Task<ISocketClient> ConnectClient(IPAddress listenAddress, int port)
        {
            Channel = await Client.BindAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }
        
        public abstract Task ShutdownClient();
    }
}