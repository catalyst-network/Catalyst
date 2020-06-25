using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Catalyst.NetworkUtils
{
    public class SocketWrapper : ISocket
    {
        private readonly Socket _socket;

        public SocketWrapper(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, socketType, protocolType);
        }

        public EndPoint LocalEndPoint => _socket.LocalEndPoint;
        public Task ConnectAsync(string host, int port)
        {
            return _socket.ConnectAsync(host, port);
        }
        
        public void Dispose()
        {
            _socket.Dispose();
        }
    }
}
