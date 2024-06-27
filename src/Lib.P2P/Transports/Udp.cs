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

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using MultiFormats;

namespace Lib.P2P.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over UDP.
    /// </summary>
    public class Udp : IPeerTransport
    {
        private static ILog _log = LogManager.GetLogger(typeof(Udp));

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address,
            CancellationToken cancel = default)
        {
            var port = address.Protocols
               .Where(p => p.Name == "udp")
               .Select(p => int.Parse(p.Value))
               .First();
            var ip = address.Protocols
               .Where(p => p.Name == "ip4" || p.Name == "ip6")
               .First();
            var socket = new Socket(
                ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                SocketType.Dgram,
                ProtocolType.Udp);

            // Handle cancellation of the connect attempt
            cancel.Register(() =>
            {
                socket.Dispose();
                socket = null;
            });

            try
            {
                _log.Debug("connecting to " + address);
                await socket.ConnectAsync(ip.Value, port).ConfigureAwait(false);
                _log.Debug("connected " + address);
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it, the caller has cancelled and doesn't care.
            }
            catch (Exception e)
            {
                _log.Warn("failed " + address, e);
                throw;
            }

            if (cancel.IsCancellationRequested)
            {
                _log.Debug("cancel " + address);
                socket?.Dispose();
                cancel.ThrowIfCancellationRequested();
            }

            return new DatagramStream(socket, true);
        }

        /// <inheritdoc />
        public MultiAddress Listen(MultiAddress address,
            Action<Stream, MultiAddress, MultiAddress> handler,
            CancellationToken cancel)
        {
            var port = address.Protocols
               .Where(p => p.Name == "udp")
               .Select(p => int.Parse(p.Value))
               .FirstOrDefault();
            var ip = address.Protocols
               .Where(p => p.Name == "ip4" || p.Name == "ip6")
               .First();
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Dgram,
                ProtocolType.Udp);
            socket.Bind(endPoint);

            // If no port specified, then add it.
            var actualPort = ((IPEndPoint) socket.LocalEndPoint).Port;
            if (port != actualPort)
            {
                address = address.Clone();
                var protocol = address.Protocols.FirstOrDefault(p => p.Name == "udp");
                if (protocol != null)
                    protocol.Value = actualPort.ToString();
                else
                    address.Protocols.AddRange(new MultiAddress("/udp/" + actualPort).Protocols);
            }

            // TODO: UDP listener
            throw new NotImplementedException();
#if false
            var stream = new DatagramStream(socket, ownsSocket: true);
            handler(stream, address, null);

            return address;
#endif
        }
    }
}
