using System;
using System.Net;
using System.Net.Sockets;
using Catalyst.Helpers.Logger;

namespace Catalyst.Helpers.Network
{
    public static class Ip
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IPAddress GetPublicIp()
        {
            string url = "http://checkip.dyndns.org";
            WebRequest req = WebRequest.Create(url);
            WebResponse resp = req.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream() ?? throw new Exception());
            string response = sr.ReadToEnd().Trim();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            string a4 = a3[0];
            return IPAddress.Parse(a4);
        }

        /// <summary>
        /// Defines a valid range of ports clients can operate on
        /// We shouldn't let clients run on privileged ports and ofc cant operate over the highest post
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool ValidPortRange(int port)
        {
            if ( port < 1025 || port > 65535)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// given an ip in a string format should validate and return a IPAddress object.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IPAddress ValidateIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) throw new ArgumentNullException(nameof(ip));

            IPAddress validIp;
            try
            {
                validIp = IPAddress.Parse(ip);
            }
            catch (ArgumentNullException e)
            {
                LogException.Message("Catalyst.Catalyst.Helpers.Network.Ip.ValidateIp", e);
                throw;
            }
            catch (FormatException e)
            {
                LogException.Message("Catalyst.Catalyst.Helpers.Network.Ip.ValidateIp", e);
                throw;
            }
            catch (SocketException e)
            {
                LogException.Message("Catalyst.Catalyst.Helpers.Network.Ip.ValidateIp", e);
                throw;
            }
            catch (Exception e)
            {
                LogException.Message("Catalyst.Catalyst.Helpers.Network.Ip.ValidateIp", e);
                throw;
            }
            if (validIp == null) throw new ArgumentNullException(nameof(ip));
            return validIp;
        }
    }
}
