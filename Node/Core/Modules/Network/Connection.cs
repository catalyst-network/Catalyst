using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using ADL.Util;

namespace ADL.Node.Core.Modules.Network
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class Connection : IDisposable
    {
        public int Port { get; }
        public string Ip { get; }
        private bool Disposed { get; set; }
        public bool Connected { set; get; }
        internal TcpClient TcpClient { get; }
        public SslStream SslStream { set; get; }
        internal NetworkStream NetworkStream { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        public Connection(TcpClient tcp)
        {
            TcpClient = tcp ?? throw new ArgumentNullException(nameof(tcp));
            Connected = true;
            NetworkStream = tcp.GetStream();
            Port = ((IPEndPoint)tcp.Client.RemoteEndPoint).Port;
            Ip = ((IPEndPoint)tcp.Client.RemoteEndPoint).Address.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Log.Log.Message("disposing connection");
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
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
                NetworkStream?.Dispose();
                TcpClient?.Dispose();
            }
            Connected = false;
            Disposed = true;
            Log.Log.Message("connection disposed");
        }
    }
}
