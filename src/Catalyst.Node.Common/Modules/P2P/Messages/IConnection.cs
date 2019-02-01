using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace Catalyst.Node.Common.Modules.P2P.Messages
{
    public interface IConnection : IDisposable
    {
        bool Connected { set; get; }
        bool Disposed { get; set; }
        TcpClient TcpClient { get; }
        IPEndPoint EndPoint { get; set; }
        SslStream SslStream { set; get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        bool IsConnected();
    }
}