using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.Helpers.Network
{
    public static class Ip
    {
        public static readonly string[] DefaultIpEchoUrls;

        static Ip()
        {
            DefaultIpEchoUrls = new[]
            {
                "https://api.ipify.org",
                "https://ipecho.net/plain",
                "https://ifconfig.co/ip",
                "https://ipv4bot.whatismyipaddress.com"
            };
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static async Task<IPAddress> GetPublicIpAsync(IObservable<string> ipEchoUrls = null)
        {
            //TODO : Build a server-less function to avoid relying on this site
            ipEchoUrls = ipEchoUrls ?? DefaultIpEchoUrls.ToObservable();
            
            var echoedIp = await ipEchoUrls
               .Select((url, i) => Observable.FromAsync(async () => await TryGetExternalIpFromEchoUrl(url)))
               .Merge()
               .FirstAsync(t => t != null);
            
            return echoedIp;
        }

        private static async Task<IPAddress> TryGetExternalIpFromEchoUrl(string url)
        {
            try
            {
                var req = WebRequest.Create(url);
                using (var response = await req.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var responseContent = (await reader.ReadToEndAsync()).Trim();
                    return IPAddress.Parse(responseContent);
                }
            }
            catch
            {
                return null;
            }
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