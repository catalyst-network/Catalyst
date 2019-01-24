using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Catalyst.Helpers.Logger;

namespace Catalyst.Helpers.IO
{
    /// <summary>
    /// </summary>
    public sealed class Connection : IDisposable
    {
        /// <summary>
        /// </summary>
        /// <param name="tcp"></param>
        public Connection(TcpClient tcp)
        {
            //@TODO guard util
            TcpClient = tcp ?? throw new ArgumentNullException(nameof(tcp));

            EndPoint = (IPEndPoint) tcp.Client.RemoteEndPoint;

            Connected = true;
        }

        public bool Connected { set; get; }
        internal bool Disposed { get; set; }
        public TcpClient TcpClient { get; }
        public IPEndPoint EndPoint { get; set; }
        public SslStream SslStream { set; get; }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Log.Message("disposing connection");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (TcpClient == null) throw new ArgumentNullException(nameof(TcpClient));

            if (!TcpClient.Connected) return false;

            if (!TcpClient.Client.Poll(0, SelectMode.SelectWrite) ||
                TcpClient.Client.Poll(0, SelectMode.SelectError)) return false;

            var buffer = new byte[1]; // @TODO hook into new byte array method && determine buffer length
            return TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                SslStream?.Dispose();
                TcpClient?.Dispose();
                EndPoint = null;
            }

            Disposed = true;
            Connected = false;
            Log.Message("connection disposed");
        }
    }
}