using System.Net;

namespace ADL.Network
{
    /// <summary>
    /// 
    /// </summary>
    public static class EndpointBuilder
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(IPAddress ip, int port)
        {
            return new IPEndPoint(ip, port);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(string ip, int port)
        {
            IPAddress validatedIp = Ip.ValidateIp(ip);
            return BuildNewEndPoint(validatedIp, port);
        }
    }
}