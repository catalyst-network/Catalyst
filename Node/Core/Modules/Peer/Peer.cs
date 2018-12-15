using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace ADL.Node.Core.Modules.Peer
{    
    /// <summary>
    /// 
    /// </summary>
    public sealed class Peer : IDisposable
    {
        public int Port { get; }
        public string Ip { get; }
        public long Nonce { set; get; } 
        private bool Disposed { get; set; }
        public bool Connected { set; get; }
        public bool Handshaked { get; set; }
        internal TcpClient TcpClient { get; }
        public DateTime LastSeen { get; set; }
        public SslStream SslStream { set; get; }
        public short ClientVersion { get; set; }
        public int Reputation { get; private set; }
        internal NetworkStream NetworkStream { get; }
        public static byte[] PublicKey { get; private set; }

        internal TimeSpan InactiveFor
        {
            get { return DateTimeProvider.UtcNow - LastSeen; }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        public Peer(TcpClient tcp)
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
