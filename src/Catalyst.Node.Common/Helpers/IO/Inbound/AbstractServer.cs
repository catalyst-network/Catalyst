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
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public abstract class AbstractServer : ISocketServer
    {
        public IChannel Channel { get; set; }
        
        public IServerBootstrp Server { get; set; }
        
        public abstract ISocketServer Bootstrap(IChannelHandler channelInitializer);

        public async Task<ISocketServer> StartServer(IPAddress listenAddress, int port)
        {
            Channel = await Server.BindAsync(listenAddress, port).ConfigureAwait(false);
            return this;
        }
        
        public abstract Task ShutdownServer();
    }
}