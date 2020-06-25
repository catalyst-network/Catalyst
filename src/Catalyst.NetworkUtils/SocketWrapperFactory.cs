using System.Net.Sockets;

namespace Catalyst.NetworkUtils
{
    public class SocketWrapperFactory : ISocketFactory
    {
        public ISocket Create(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            return new SocketWrapper(addressFamily,socketType, protocolType);
        }
    }
}
