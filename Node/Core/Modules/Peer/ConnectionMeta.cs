using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace ADL.Node.Core.Modules.Peer
{    
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionMeta : IDisposable
    {
        public int Port { set; get; }
        public string Ip { set; get; }
        public long nonce { set; get; }
        private bool Disposed { get; set; }
        public bool _Connected { set; get; }
        public SslStream SslStream { set; get; }
        internal TcpClient TcpClient { set; get; }
        internal NetworkStream NetworkStream { set; get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        public ConnectionMeta(TcpClient tcp)
        {
            TcpClient = tcp ?? throw new ArgumentNullException(nameof(tcp));
            _Connected = true;
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
        protected virtual void Dispose(bool disposing)
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

            _Connected = false;
            Disposed = true;
        }
    }
}
