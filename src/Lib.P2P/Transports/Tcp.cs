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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using MultiFormats;

namespace Lib.P2P.Transports
{
    /// <summary>
    ///   Establishes a duplex stream between two peers
    ///   over TCP.
    /// </summary>
    /// <remarks>
    ///   <see cref="ConnectAsync"/> determines the network latency and sets the timeout
    ///   to 3 times the latency or <see cref="MinReadTimeout"/>.
    /// </remarks>
    public sealed class Tcp : IPeerTransport
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Tcp));

        /// <summary>
        ///  The minimum read timeout.
        /// </summary>
        /// <value>
        ///   Defaults to 3 seconds.
        /// </value>
        private static readonly TimeSpan MinReadTimeout = TimeSpan.FromSeconds(3);

        /// <inheritdoc />
        public async Task<Stream> ConnectAsync(MultiAddress address,
            CancellationToken cancel = default)
        {
            var port = address.Protocols
               .Where(p => p.Name == "tcp")
               .Select(p => int.Parse(p.Value))
               .First();
            
            var ip = address.Protocols
               .FirstOrDefault(p => p.Name == "ip4" || p.Name == "ip6");
            
            if (ip == null)
            {
                throw new ArgumentException($"Missing IP address in '{address}'.", nameof(address));
            }
            
            var socket = new Socket(
                ip.Name == "ip4" ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6,
                SocketType.Stream,
                ProtocolType.Tcp);

            var latency = MinReadTimeout; // keep compiler happy
            var start = DateTime.Now;
            
            try
            {
                Log.Trace("connecting to " + address);

                // Handle cancellation of the connect attempt by disposing
                // of the socket.  This will force ConnectAsync to return.
                await using (var _ = cancel.Register(() =>
                {
                    if (socket != null)
                    {
                        socket?.Dispose();
                    }
                    
                    socket = null;
                }))
                {
                    var ipaddr = IPAddress.Parse(ip.Value);
                    await socket.ConnectAsync(ipaddr, port).ConfigureAwait(false);
                }

                latency = DateTime.Now - start;
                Log.Trace($"connected to {address} in {latency.TotalMilliseconds} ms");
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // eat it, the caller has cancelled and doesn't care.
            }
            catch (Exception)
            {
                latency = DateTime.Now - start;
                Log.Trace($"failed to {address} in {latency.TotalMilliseconds} ms");
                socket?.Dispose();
                throw;
            }

            if (cancel.IsCancellationRequested)
            {
                Log.Trace("cancel " + address);
                socket?.Dispose();
                cancel.ThrowIfCancellationRequested();
            }

            var timeout = (int) Math.Max(MinReadTimeout.TotalMilliseconds, latency.TotalMilliseconds * 3);
            socket.LingerState = new LingerOption(false, 0);
            socket.ReceiveTimeout = timeout;
            socket.SendTimeout = timeout;
            Stream stream = new NetworkStream(socket, true);
            stream.ReadTimeout = timeout;
            stream.WriteTimeout = timeout;

            stream = new DuplexBufferedStream(stream);

            if (!cancel.IsCancellationRequested)
            {
                return stream;
            }
            
            Log.Trace("cancel " + address);
            await stream.DisposeAsync();
            cancel.ThrowIfCancellationRequested();

            return stream;
        }

        /// <inheritdoc />
        public MultiAddress Listen(MultiAddress address,
            Action<Stream, MultiAddress, MultiAddress> handler,
            CancellationToken cancel)
        {
            var port = address.Protocols
               .Where(p => p.Name == "tcp")
               .Select(p => int.Parse(p.Value))
               .FirstOrDefault();
            
            var ip = address.Protocols
               .FirstOrDefault(p => p.Name == "ip4" || p.Name == "ip6");
            
            if (ip == null)
            {
                throw new ArgumentException($"Missing IP address in '{address}'.", nameof(address));
            }
            
            var ipAddress = IPAddress.Parse(ip.Value);
            var endPoint = new IPEndPoint(ipAddress, port);
            var socket = new Socket(
                endPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);
            try
            {
                socket.Bind(endPoint);
                socket.Listen(100);
            }
            catch (Exception e)
            {
                socket.Dispose();
                throw new Exception("Bind/listen failed on " + address, e);
            }

            // If no port specified, then add it.
            var actualPort = ((IPEndPoint) socket.LocalEndPoint).Port;
            
            if (port != actualPort)
            {
                address = address.Clone();
                var protocol = address.Protocols.FirstOrDefault(p => p.Name == "tcp");
                if (protocol != null)
                {
                    protocol.Value = actualPort.ToString();
                }
                else
                {
                    address.Protocols.AddRange(new MultiAddress("/tcp/" + actualPort).Protocols);
                }
            }

            _ = Task.Run(() => ProcessConnection(socket, address, handler, cancel), cancel);

            return address;
        }

        private static void ProcessConnection(Socket socket,
            MultiAddress address,
            Action<Stream, MultiAddress, MultiAddress> handler,
            CancellationToken cancel)
        {
            Log.Debug("listening on " + address);

            // Handle cancellation of the listener
            cancel.Register(() =>
            {
                Log.Debug("Got cancel on " + address);

                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Dispose();
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        socket.Shutdown(SocketShutdown.Receive);
                    }
                    else // must be windows
                    {
                        socket.Dispose();
                    }
                }
                catch (Exception e)
                {
                    Log.Warn($"Cancelling listener: {e.Message}");
                }
                finally
                {
                    socket = null;
                }
            });

            try
            {
                while (!cancel.IsCancellationRequested)
                {
                    var conn = socket.Accept();

                    MultiAddress remote = null;
                    if (conn.RemoteEndPoint is IPEndPoint endPoint)
                    {
                        var s = new StringBuilder();
                        s.Append(endPoint.AddressFamily == AddressFamily.InterNetwork ? "/ip4/" : "/ip6/");
                        s.Append(endPoint.Address);
                        s.Append("/tcp/");
                        s.Append(endPoint.Port);
                        remote = new MultiAddress(s.ToString());
                        Log.Debug("connection from " + remote);
                    }

                    conn.NoDelay = true;
                    Stream peer = new NetworkStream(conn, true);

                    peer = new DuplexBufferedStream(peer);

                    try
                    {
                        handler(peer, address, remote);
                    }
                    catch (Exception e)
                    {
                        Log.Error("listener handler failed " + address, e);
                        peer.Dispose();
                    }
                }
            }
            catch (Exception) when (cancel.IsCancellationRequested)
            {
                // ignore
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Log.Error("listener failed " + address, e);
            }
            finally
            {
                socket?.Dispose();
            }

            Log.Debug("stop listening on " + address);
        }
    }
}
