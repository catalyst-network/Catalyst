using System;
using System.Net;
using System.Threading.Tasks;
using Catalyst.Node.Common.Interfaces;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Handlers.Logging;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO.Outbound
{
    public sealed class TcpClient : IDisposable, ISocketClient
    {
        private readonly ILogger _logger;
        public IChannel Channel { get; set; }
        public Bootstrap Client { get; set; }
        private IEventLoopGroup ClientEventLoopGroup { get; set; }

        public TcpClient(ILogger logger)
        {
            _logger = logger;
        }

        public ISocketClient Bootstrap(IChannelHandler channelInitializer)
        {
            ClientEventLoopGroup = new MultithreadEventLoopGroup();
            Client = new Bootstrap()
               .Group(ClientEventLoopGroup)
               .Channel<TcpSocketChannel>()
               .Option(ChannelOption.SoBacklog, 100)
               .Handler(new LoggingHandler(LogLevel.INFO))
               .Handler(channelInitializer);
            return this;
        }
        
        public async Task<ISocketClient> StartClient(IPAddress targetHost, int port)
        {
            Channel = await Client.ConnectAsync(new IPEndPoint(targetHost, port));
            return this;
        }

        public async Task ShutdownClient()
        {
            if (Channel != null)
            {
                await Channel.CloseAsync().ConfigureAwait(false);
            }
            if (ClientEventLoopGroup != null )
            {
                await ClientEventLoopGroup.ShutdownGracefullyAsync().ConfigureAwait(false);
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _logger.Information("Disposing TCP Client");
                Task.WaitAll(ShutdownClient());
            }
        }
    }
}
