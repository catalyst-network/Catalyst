using System.Net;
using System.Net.Sockets;

namespace ADL.Node.Core.Modules.Network.Listeners
{
    public static class TcpListenerFactory
    {
        /// <summary>
        /// returns a TcpListener
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <returns></returns>
        public static TcpListener CreateListener(IPEndPoint ipEndPoint)
        {
            return new TcpListener(ipEndPoint.Address, ipEndPoint.Port); // we need to pass our own node identifier to a function that creates a listener
        }
    }
}