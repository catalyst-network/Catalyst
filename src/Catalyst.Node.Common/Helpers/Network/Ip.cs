/*
 * Copyright (c) 2019 Catalyst Network
 *
 * This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
 *
 * Catalyst.Node is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 2 of the License, or
 * (at your option) any later version.
 * 
 * Catalyst.Node is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Catalyst.Node.Common.Helpers.Network
{
    public static class Ip
    {
        public static readonly ReadOnlyCollection<string> DefaultIpEchoUrls;

        static Ip()
        {
            DefaultIpEchoUrls = new List<string>
            {
                "https://api.ipify.org",
                "https://ipecho.net/plain",
                "https://ifconfig.co/ip",
                "https://ipv4bot.whatismyipaddress.com"
            }.AsReadOnly();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static async Task<IPAddress> GetPublicIpAsync(IObservable<string> ipEchoUrls = null)
        {
            //TODO : Build a server-less function to avoid relying on these sites
            var defaultedIpEchoUrls = ipEchoUrls ?? DefaultIpEchoUrls.ToObservable();

            var echoedIp = await defaultedIpEchoUrls
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
                using (var reader =
                    new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException()))
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
        public static bool ValidPortRange(int port) { return 1025 <= port && port <= 65535; }

        /// <summary>
        /// </summary>
        /// <param name="ipOrHost"></param>
        /// <returns></returns>
        public static IPAddress BuildIpAddress(string ipOrHost)
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