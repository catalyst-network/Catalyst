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
        public bool Known { get; set; }
        public bool Connected { set; get; }
        internal bool Disposed { get; set; }
        internal TcpClient TcpClient { get; }
        public IPEndPoint EndPoint { get; set; }
        public SslStream SslStream { set; get; }
        internal NetworkStream NetworkStream { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tcp"></param>
        public Connection(TcpClient tcp)
        {
            TcpClient = tcp ?? throw new ArgumentNullException(nameof(tcp));

            try
            {
                NetworkStream = tcp.GetStream();
            }
            catch (ObjectDisposedException e)
            {
                Log.LogException.Message("Connection Constructor: tcp stream object disposed", e);
                throw;
            }
            catch (InvalidOperationException e)
            {
                Log.LogException.Message("Connection Constructor: tcp stream invalid operation", e);
                throw;
            }

            EndPoint = (IPEndPoint) tcp.Client.RemoteEndPoint;
            
            Connected = true;
            Known = false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal bool IsConnected()
        {
            if (TcpClient == null) throw new ArgumentNullException(nameof(TcpClient));
            
            if (!TcpClient.Connected)
            {
                return false;
            }
            
            if (!TcpClient.Client.Poll(0, SelectMode.SelectWrite) || TcpClient.Client.Poll(0, SelectMode.SelectError))
            {
                return false;
            }
            
            byte[] buffer = new byte[1];// @TODO hook into new byte array method
            return TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
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
                EndPoint = null;
            }
            
            Disposed = true;
            Connected = false;
            Log.Log.Message("connection disposed");
        }
    }
}
