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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MultiFormats;

namespace Lib.P2P
{
    /// <summary>
    ///   Extensions to <see cref="MultiAddress"/>.
    /// </summary>
    public static class MultiAddressExtensions
    {
        private static Dictionary<AddressFamily, string> _supportedDnsAddressFamilies =
            new Dictionary<AddressFamily, string>();

        private static MultiAddress _http = new MultiAddress("/tcp/80");
        private static MultiAddress _https = new MultiAddress("/tcp/443");

        static MultiAddressExtensions()
        {
            if (Socket.OSSupportsIPv4)
                _supportedDnsAddressFamilies[AddressFamily.InterNetwork] = "/ip4/";
            if (Socket.OSSupportsIPv6)
                _supportedDnsAddressFamilies[AddressFamily.InterNetworkV6] = "/ip6/";
        }

        /// <summary>
        ///   Determines if the multiaddress references
        ///   a loopback address.
        /// </summary>
        /// <param name="multiaddress">
        ///   The mutiaddress to clone.
        /// </param>
        /// <returns>
        ///   <b>true</b> for a loopback (127.0.0.1 or ::1).
        /// </returns>
        public static bool IsLoopback(this MultiAddress multiaddress)
        {
            return multiaddress.Protocols.Any(p =>
                p.Name == "ip4" && p.Value == "127.0.0.1" ||
                p.Name == "ip6" && p.Value == "::1");
        }

        /// <summary>
        ///   Get all the addresses for the specified <see cref="MultiAddress"/>.
        /// </summary>
        /// <param name="multiaddress">
        ///   The multiaddress to resolve.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's result
        ///   is a sequence of possible multiaddresses.
        /// </returns>
        /// <exception cref="SocketException">
        ///   The host name cannot be resolved.
        /// </exception>
        /// <remarks>
        ///   When the <see cref="NetworkProtocol.Name"/> starts with "dns", then a DNS
        ///   lookup is performed to get all the IP addresses for the host name.  "dn4" and "dns6"
        ///   will filter the result for IPv4 and IPV6 addresses.
        ///   <para>
        ///   When the <see cref="NetworkProtocol.Name"/> is "http" or "https", then
        ///   a "tcp/80" or "tcp/443" is respectively added.
        ///   </para>
        /// </remarks>
        public static async Task<List<MultiAddress>> ResolveAsync(this MultiAddress multiaddress,
            CancellationToken cancel = default)
        {
            var list = new List<MultiAddress>();

            // HTTP
            var i = multiaddress.Protocols.FindIndex(ma => ma.Name == "http");
            if (i >= 0 && !multiaddress.Protocols.Any(p => p.Name == "tcp"))
            {
                multiaddress = multiaddress.Clone();
                multiaddress.Protocols.InsertRange(i + 1, _http.Protocols);
            }

            // HTTPS
            i = multiaddress.Protocols.FindIndex(ma => ma.Name == "https");
            if (i >= 0 && !multiaddress.Protocols.Any(p => p.Name == "tcp"))
            {
                multiaddress = multiaddress.Clone();
                multiaddress.Protocols.InsertRange(i + 1, _https.Protocols);
            }

            // DNS*
            i = multiaddress.Protocols.FindIndex(ma => ma.Name.StartsWith("dns"));
            if (i < 0)
            {
                list.Add(multiaddress);
                return list;
            }

            var protocolName = multiaddress.Protocols[i].Name;
            var host = multiaddress.Protocols[i].Value;

            // TODO: Don't use DNS, but use the IPFS Engine DNS resolver.
            // This will not then expose the domain name in plain text.
            // We also, then get to specify if A and/or AAAA records are needed.
            var addresses = (await Dns.GetHostAddressesAsync(host).ConfigureAwait(false))
               .Where(a => _supportedDnsAddressFamilies.ContainsKey(a.AddressFamily))
               .Where(a =>
                    protocolName == "dns" ||
                    protocolName == "dns4" && a.AddressFamily == AddressFamily.InterNetwork ||
                    protocolName == "dns6" && a.AddressFamily == AddressFamily.InterNetworkV6);
            foreach (var addr in addresses)
            {
                var ma0 = new MultiAddress(_supportedDnsAddressFamilies[addr.AddressFamily] + addr);
                var ma1 = multiaddress.Clone();
                ma1.Protocols[i] = ma0.Protocols[0];
                list.Add(ma1);
            }

            return list;
        }
    }
}
