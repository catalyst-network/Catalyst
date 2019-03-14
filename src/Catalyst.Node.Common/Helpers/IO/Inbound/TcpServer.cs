using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Tls;

namespace Catalyst.Node.Common.Helpers.IO.Inbound
{
    public class TcpServer
    {
        public IChannel Channel { get; set; }
        
        private readonly int _port;
        private readonly IPAddress _listenAddress;
        private readonly IEventLoopGroup _supervisorEventLoop;
        private readonly IEventLoopGroup _workerEventLoop;
        private readonly IChannelHandler _channelHandler;
        private readonly X509Certificate _certificate;

        public TcpServer
        (
            int port,
            IPAddress listenAddress,
            IEventLoopGroup supervisorEventLoop,
            IEventLoopGroup workerEventLoop,
            IChannelHandler channelHandler
        )
        {
            _port = port;
            _listenAddress = listenAddress;
            _supervisorEventLoop = supervisorEventLoop;
            _workerEventLoop = workerEventLoop;
            _channelHandler = channelHandler;
        }

        public async Task<IChannel> StartServer(X509Certificate certificate, IChannelHandler channelInitializer)
        {
            var encoder = new StringEncoder(Encoding.UTF8);
            var decoder = new StringDecoder(Encoding.UTF8);

            var bootstrap = new ServerBootstrap();
            bootstrap
               .Group(_supervisorEventLoop, _workerEventLoop)
               .Channel<TcpServerSocketChannel>()
               .Option(ChannelOption.SoBacklog, 100)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .ChildHandler(channelInitializer);

            return await bootstrap.BindAsync(_port);
        }
    }
}