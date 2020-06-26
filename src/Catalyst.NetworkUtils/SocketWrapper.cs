#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

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
