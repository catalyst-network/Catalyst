using System;
using System.Net;
using System.Net.Sockets;

namespace Catalyst.Node.Core.Modules.P2P.Listeners
{
    public static class ListenerFactory
    {
        /// <summary>
        ///     returns a TcpListener
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        public static TcpListener CreateTcpListener(IPEndPoint ipEndPoint)
        {
            //@TODO put in try catch
            return new TcpListener(ipEndPoint.Address, ipEndPoint.Port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static UdpClient CreateUdpListener()
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static TcpListener CreateTorListener()
        {
            throw new NotImplementedException();
        }
    }
}