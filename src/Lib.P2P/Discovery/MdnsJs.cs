#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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
using Makaretu.Dns;
using MultiFormats;

namespace Lib.P2P.Discovery
{
    /// <summary>
    ///   Discovers peers using Multicast DNS according to
    ///   js-ipfs v0.32.3
    /// </summary>
    public class MdnsJs : Mdns
    {
        /// <summary>
        ///   Creates a new instance of the class.  Sets the <see cref="Mdns.ServiceName"/>
        ///   to "ipfs".
        /// </summary>
        public MdnsJs() { ServiceName = "ipfs"; }

        /// <inheritdoc />
        protected override void OnServiceDiscovery(ServiceDiscovery discovery)
        {
            discovery.AnswersContainsAdditionalRecords = true;
        }

        /// <inheritdoc />
        public override ServiceProfile BuildProfile()
        {
            // Only internet addresses.
            var addresses = LocalPeer.Addresses
               .Where(a => a.Protocols.First().Name == "ip4" || a.Protocols.First().Name == "ip6")
               .ToArray();
            if (addresses.Length == 0) return null;
            var ipAddresses = addresses
               .Select(a => IPAddress.Parse(a.Protocols.First().Value));

            // Only one port is supported.
            var tcpPort = addresses.First()
               .Protocols.First(p => p.Name == "tcp")
               .Value;

            // Create the DNS records for this peer.  The TXT record
            // is singular and must contain the peer ID.
            ServiceProfile profile = new(
                LocalPeer.Id.ToBase58(),
                ServiceName,
                ushort.Parse(tcpPort),
                ipAddresses
            );
            profile.Resources.RemoveAll(r => r.Type == DnsType.TXT);
            var txt = new TXTRecord {Name = profile.FullyQualifiedName};
            txt.Strings.Add(profile.InstanceName.ToString());
            profile.Resources.Add(txt);

            return profile;
        }

        /// <inheritdoc />
        public override IEnumerable<MultiAddress> GetAddresses(Message message)
        {
            var qsn = ServiceName + ".local";
            var peerNames = message.Answers
               .OfType<PTRRecord>()
               .Where(a => a.Name == qsn)
               .Select(a => a.DomainName);
            foreach (var name in peerNames)
            {
                var id = name.Labels[0];
                var srv = message.Answers
                   .OfType<SRVRecord>()
                   .First(r => r.Name == name);
                var aRecords = message.Answers
                   .OfType<ARecord>()
                   .Where(a => a.Name == name || a.Name == srv.Target);
                foreach (var a in aRecords) yield return new MultiAddress($"/ip4/{a.Address}/tcp/{srv.Port}/ipfs/{id}");
                var aaaaRecords = message.Answers
                   .OfType<AAAARecord>()
                   .Where(a => a.Name == name || a.Name == srv.Target);
                foreach (var a in aaaaRecords)
                    yield return new MultiAddress($"/ip6/{a.Address}/tcp/{srv.Port}/ipfs/{id}");
            }
        }
    }
}
