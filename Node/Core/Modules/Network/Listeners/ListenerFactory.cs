using System;
using System.Net;
using System.Net.Sockets;

namespace ADL.Node.Core.Modules.Network.Listeners
{
    public static class ListenerFactory
    {
        /// <summary>
        /// returns a TcpListener
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        public static TcpListener CreateTcpListener(IPEndPoint ipEndPoint)
        {
            return new TcpListener(ipEndPoint.Address, ipEndPoint.Port);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static TcpListener CreateTorListener()
        {
            throw new NotImplementedException();
        }
    }
}