using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;

namespace Catalyst.Node.Common.Helpers.IO.Outbound
{
    public class TcpClient
    {
        private readonly int _port;
        private readonly IPAddress _listenAddress;
        private readonly IEventLoopGroup _supervisorEventLoop;
        private readonly IEventLoopGroup _workerEventLoop;

        public TcpClient
        (
            int port,
            IPAddress listenAddress,
            IEventLoopGroup supervisorEventLoop,
            IEventLoopGroup workerEventLoop
        )
        {
            _port = port;
            _listenAddress = listenAddress;
            _supervisorEventLoop = supervisorEventLoop;
            _workerEventLoop = workerEventLoop;
        }
        
        public async Task<IChannel> StartClient(IChannelHandler channelInitializer)
        {
            var bootstrap = new ServerBootstrap();
            bootstrap
               .Group(_supervisorEventLoop, _workerEventLoop)
               .Channel<TcpServerSocketChannel>()
               .Option(ChannelOption.SoBacklog, 100)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .ChildHandler(channelInitializer);

            return await bootstrap.BindAsync(_listenAddress, _port);
        }
    }
}
