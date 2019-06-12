using Catalyst.Common.Interfaces.IO;
using Dawn;
using DotNetty.Transport.Channels;

namespace Catalyst.Common.IO
{
    public class HandlerWorkerEventLoopGroupFactory : IHandlerWorkerEventLoopGroupFactory
    {
        private readonly int _tcpClientThreads;
        private readonly int _tcpServerThreads;
        private readonly int _udpServerThreads;
        private readonly int _udpClientThreads;

        public HandlerWorkerEventLoopGroupFactory(int tcpClientThreads = 0,
            int tcpServerThreads = 0,
            int udpServerThreads = 0,
            int udpClientThreads = 0)
        {
            _tcpClientThreads = tcpClientThreads;
            _tcpServerThreads = tcpServerThreads;
            _udpClientThreads = udpClientThreads;
            _udpServerThreads = udpServerThreads;
        }

        public MultithreadEventLoopGroup NewTcpClientLoopGroup()
        {
            Guard.Argument(_tcpClientThreads).Positive();
            return new MultithreadEventLoopGroup(_tcpClientThreads);
        }

        public MultithreadEventLoopGroup NewTcpServerLoopGroup()
        {
            Guard.Argument(_tcpServerThreads).Positive();
            return new MultithreadEventLoopGroup(_tcpServerThreads);
        }

        public MultithreadEventLoopGroup NewUdpServerLoopGroup()
        {
            Guard.Argument(_udpServerThreads).Positive();
            return new MultithreadEventLoopGroup(_udpServerThreads);
        }

        public MultithreadEventLoopGroup NewUdpClientLoopGroup()
        {
            Guard.Argument(_udpClientThreads).Positive();
            return new MultithreadEventLoopGroup(_udpClientThreads);
        }
    }
}
