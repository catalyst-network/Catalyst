#region LICENSE

/**
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

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Catalyst.Core.Util;
using Serilog;

namespace Catalyst.Core.Network
{
    public static class Ip
    {
        private static readonly ILogger Logger = Log.Logger.ForContext(MethodBase.GetCurrentMethod().DeclaringType);

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
            var defaultedIpEchoUrls = ipEchoUrls ?? DefaultIpEchoUrls.ToObservable();

            var echoedIp = await defaultedIpEchoUrls
               .Select((url, i) => Observable.FromAsync(async () => await TryGetExternalIpFromEchoUrlAsync(url).ConfigureAwait(false)))
               .Merge()
               .FirstAsync(t => t != null);

            return echoedIp;
        }

        /// <summary>
        ///     Creates a standardised format byte array that can handle a IPv6 address or an IPv4 with leading bytes padded with 0x0
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static byte[] To16Bytes(this IPAddress address)
        {
            var ipChunk = ByteUtil.InitialiseEmptyByteArray(16);
            var ipBytes = address.GetAddressBytes();

            if (ipBytes.Length == 4)
            {
                Buffer.BlockCopy(ipBytes, 0, ipChunk, 12, 4);
            }
            else
            {
                ipChunk = ipBytes;
            }

            Logger.Verbose(string.Join(" ", ipChunk));

            return ipChunk;
        }

        private static async Task<IPAddress> TryGetExternalIpFromEchoUrlAsync(string url)
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
        internal static bool ValidPortRange(int port) { return 1025 <= port && port <= 65535; }

        /// <summary>
        /// </summary>
        /// <param name="ipOrHost"></param>
        /// <returns></returns>
        public static IPAddress BuildIpAddress(string ipOrHost)
        {
            return IPAddress.TryParse(ipOrHost, out var address)
                ? address
                : Dns.GetHostAddressesAsync(ipOrHost).Result
                   .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        /// <summary>
        ///     given an ip in a string format should validate and return a IPAddress object.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static IPAddress ValidateIp(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            return IPAddress.Parse(ip);
        }
    }
}
