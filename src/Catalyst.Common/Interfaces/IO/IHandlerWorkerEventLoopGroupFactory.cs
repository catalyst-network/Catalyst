using DotNetty.Transport.Channels;

namespace Catalyst.Common.Interfaces.IO
{
    /// <summary>
    /// Creates handler worker multi-threaded loop groups
    /// </summary>
    public interface IHandlerWorkerEventLoopGroupFactory
    {
        /// <summary>Creates new multi-threaded tcp client worker group.</summary>
        /// <returns></returns>
        MultithreadEventLoopGroup NewTcpClientLoopGroup();

        /// <summary>Creates new multi-threaded tcp server worker group.</summary>
        /// <returns></returns>
        MultithreadEventLoopGroup NewTcpServerLoopGroup();

        /// <summary>Creates new multi-threaded udp server worker group.</summary>
        /// <returns></returns>
        MultithreadEventLoopGroup NewUdpServerLoopGroup();

        /// <summary>Creates new multi-threaded udp server worker group.</summary>
        /// <returns></returns>
        MultithreadEventLoopGroup NewUdpClientLoopGroup();
    }
}
