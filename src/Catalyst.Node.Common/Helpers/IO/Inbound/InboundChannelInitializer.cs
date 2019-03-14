using System;
using System.Security.Cryptography.X509Certificates;
using DotNetty.Codecs;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Channels;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public sealed class InboundChannelInitializer<T> : ChannelInitializer<T> where T : IChannel
    {
        private readonly Action<T> _initializationAction;
        private readonly X509Certificate _certificate;
        private readonly IChannelHandler _encoder;
        private readonly IChannelHandler _decoder;
        private readonly IChannelHandler _channelHandler;
        
        /// <summary>
        ///     Generic inbound channel initializer for tls sockets
        /// </summary>
        /// <param name="initializationAction"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="channelHandler"></param>
        /// <param name="certificate"></param>
        public InboundChannelInitializer(Action<T> initializationAction, IChannelHandler encoder, IChannelHandler decoder, IChannelHandler channelHandler, X509Certificate certificate)
        {
            _initializationAction = initializationAction;
            _certificate = certificate;
            _encoder = encoder;
            _decoder = decoder;
            _channelHandler = channelHandler;
        }
        
        /// <summary>
        ///     Generic inbound channel initializer for sockets
        /// </summary>
        /// <param name="initializationAction"></param>
        /// <param name="encoder"></param>
        /// <param name="decoder"></param>
        /// <param name="channelHandler"></param>
        public InboundChannelInitializer(Action<T> initializationAction, IChannelHandler encoder, IChannelHandler decoder, IChannelHandler channelHandler)
        {
            _initializationAction = initializationAction;
            _encoder = encoder;
            _decoder = decoder;
            _channelHandler = channelHandler;
        }

        protected override void InitChannel(T channel)
        {
            _initializationAction(channel);
            var pipeline = channel.Pipeline;

            if (_certificate != null)
            {
                pipeline.AddLast(TlsHandler.Server(_certificate));
            }

            pipeline.AddLast(new LoggingHandler(LogLevel.DEBUG));
            pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Delimiters.LineDelimiter()));
            pipeline.AddLast(_encoder, _decoder, _channelHandler);
        }

        public override string ToString()
        {
            return "InboundChannelInitializer[" + StringUtil.SimpleClassName(typeof (T)) + "]";
        }
    }
}