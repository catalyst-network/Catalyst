using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace Catalyst.Node.Core.Helpers.Network
{
    public static class Ip
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetPublicIp()
        {
            var url = "http://checkip.dyndns.org";
            var req = WebRequest.Create(url);
            var resp = req.GetResponse();
            var sr = new StreamReader(resp.GetResponseStream() ?? throw new ArgumentNullException());
            var response = sr.ReadToEnd().Trim();
            var a = response.Split(':');
            var a2 = a[1].Substring(1);
            var a3 = a2.Split('<');
            var a4 = a3[0];
            return IPAddress.Parse(a4);
        }

        /// <summary>
        ///     Defines a valid range of ports clients can operate on
        ///     We shouldn't let clients run on privileged ports and ofc cant operate over the highest post
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool ValidPortRange(int port)
        {
            if (port < 1025 || port > 65535)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="ipOrHost"></param>
        /// <returns></returns>
        public static IPAddress BuildIPAddress(string ipOrHost)
        {
            return IPAddress.TryParse(ipOrHost, out var address)
                       ? address
                       : System.Net.Dns.GetHostAddressesAsync(ipOrHost).Result
                               .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        /// <summary>
        ///     given an ip in a string format should validate and return a IPAddress object.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPAddress ValidateIp(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }
            return IPAddress.Parse(ip);
        }
    }
}