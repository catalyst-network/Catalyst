using System;
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
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (!Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));
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
            if (ip == null) throw new ArgumentNullException(nameof(ip));
            if (!Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));
            IPAddress validatedIp;
            try
            {
                validatedIp = Ip.ValidateIp(ip);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine(e);
                throw;
            }
            return BuildNewEndPoint(validatedIp, port);
        }
    }
}