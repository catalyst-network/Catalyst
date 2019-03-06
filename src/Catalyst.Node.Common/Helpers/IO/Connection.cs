using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using Catalyst.Node.Common.Interfaces;
using Dawn;
using Serilog;

namespace Catalyst.Node.Common.Helpers.IO
{
    /// <summary>
    /// </summary>
    public sealed class Connection : IDisposable, IConnection
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// </summary>
        /// <param name="tcpClient"></param>
        public Connection(TcpClient tcpClient)
        {
            Guard.Argument(tcpClient, nameof(tcpClient)).NotNull();
            TcpClient = tcpClient;
            EndPoint = (IPEndPoint) tcpClient.Client.RemoteEndPoint;
            Connected = true;
        }

        public bool Connected { set; get; }
        public bool Disposed { get; set; }
        public TcpClient TcpClient { get; }
        public IPEndPoint EndPoint { get; set; }
        public SslStream SslStream { set; get; }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            if (TcpClient == null)
            {
                throw new ArgumentNullException(nameof(TcpClient));
            }

            if (!TcpClient.Connected)
            {
                return false;
            }

            if (!TcpClient.Client.Poll(0, SelectMode.SelectWrite) ||
                TcpClient.Client.Poll(0, SelectMode.SelectError))
            {
                return false;
            }

            var buffer = new byte[1]; // @TODO hook into new byte array method && determine buffer length
            return TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Logger.Verbose("disposing connection");
            Dispose(true);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                SslStream?.Dispose();
                TcpClient?.Dispose();
                EndPoint = null;
            }

            Disposed = true;
            Connected = false;
            Logger.Verbose("connection disposed");
        }
    }
}