/*
* Copyright(c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node<https: //github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
* GNU General Public License for more details.
* 
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node.If not, see<https: //www.gnu.org/licenses/>.
*/

using System;
using System.Security.Cryptography.X509Certificates;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public sealed class InboundChannelInitializer<T> : AbstractChannelInitializer<T> where T : IChannel
    {
        
        /// <summary>
        ///     Generic inbound channel initializer for tls sockets
        /// </summary>
        /// <param name="initializationAction"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="channelHandler"></param>
        /// <param name="certificate"></param>
        public InboundChannelInitializer(
            Action<T> initializationAction,
            IChannelHandler encoder,
            IChannelHandler decoder,
            IChannelHandler channelHandler,
            X509Certificate certificate
        ) : base(initializationAction, encoder, decoder, channelHandler, certificate) { }
        
        /// <summary>
        ///     Generic inbound channel initializer for sockets
        /// </summary>
        /// <param name="initializationAction"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="channelHandler"></param>
        public InboundChannelInitializer(
            Action<T> initializationAction,
            IChannelHandler encoder,
            IChannelHandler decoder,
            IChannelHandler channelHandler
        ) : base(initializationAction, encoder, decoder, channelHandler) { }

        protected override void InitChannel(T channel)
        {
            InitializationAction(channel);
            var pipeline = channel.Pipeline;

            if (Certificate != null)
            {
                pipeline.AddLast(TlsHandler.Server(Certificate));
            }

            pipeline.AddLast(new LoggingHandler(LogLevel.DEBUG));
            pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
            pipeline.AddLast(Encoder, Decoder, ChannelHandler);
        }

        public override string ToString()
        {
            return "InboundChannelInitializer[" + StringUtil.SimpleClassName(typeof (T)) + "]";
        }
    }
}