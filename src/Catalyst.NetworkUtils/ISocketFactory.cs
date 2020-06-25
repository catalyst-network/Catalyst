using System;
using System.Net.Sockets;

namespace Catalyst.NetworkUtils
{
    public interface ISocketFactory
    {
        ISocket Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);
    }
}
