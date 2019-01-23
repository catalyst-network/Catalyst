using System;
using System.Globalization;
using System.Net;
using Catalyst.Helpers.Logger;

namespace Catalyst.Helpers.Network
{
    /// <summary>
    /// </summary>
    public static class EndpointBuilder
    {
        
        /// <summary>
        /// Handles IPv4 and IPv6 notation. of an ip:port string
        /// </summary>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        /// <exception cref="FormatException"></exception>
        public static IPEndPoint BuildNewEndPoint(string endPoint)
        {
            Console.WriteLine(endPoint);
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip address");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip address");
                }
            }

            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out var port))
            {
                throw new FormatException("Invalid port");
            }
            return BuildNewEndPoint(ip, port);
        }
        
        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(IPAddress ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip)); //@TODO guard
            if (!Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port));  //@TODO SEE ValidPortRange
            return new IPEndPoint(ip, port);
        }

        /// <summary>
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static IPEndPoint BuildNewEndPoint(string ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip)); //@TODO guard
            if (!Ip.ValidPortRange(port)) throw new ArgumentOutOfRangeException(nameof(port)); //@TODO SEE ValidPortRange
            IPAddress validatedIp;
            try
            {
                validatedIp = Ip.ValidateIp(ip);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("BuildNewEndPoint", e);
                throw;
            }

            return BuildNewEndPoint(validatedIp, port);
        }
    }
}