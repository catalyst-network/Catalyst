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

using System.Security.Cryptography.X509Certificates;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class InboundSessionInitializer : ChannelInitializer<ISocketChannel>
    {
        public IInboundSession InboundSession { get; }
        private readonly X509Certificate _x509Certificate;

        public InboundSessionInitializer(IInboundSession inboundSession, X509Certificate x509Certificate)
        {
            InboundSession = inboundSession;
            _x509Certificate = x509Certificate;
        }
        
        public InboundSessionInitializer(IInboundSession inboundSession)
        {
            InboundSession = inboundSession;
        }
        
        protected override void InitChannel(ISocketChannel channel)
        {
            var pipeline = channel.Pipeline;
            
            if (_x509Certificate != null)
            {
                pipeline.AddLast("tls", TlsHandler.Server(_x509Certificate));                                        
            }
            
            pipeline.AddLast(new LoggingHandler("SRV-CONN"));
            pipeline.AddLast("echo", new InboundSessionHandler(InboundSession));
        } 
    }
}