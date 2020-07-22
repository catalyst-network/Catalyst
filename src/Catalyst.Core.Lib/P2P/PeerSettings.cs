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
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Network;
using Catalyst.NetworkUtils;
using Catalyst.Protocol.Network;
using Dawn;
using Lib.P2P;
using Microsoft.Extensions.Configuration;
using MultiFormats;

namespace Catalyst.Core.Lib.P2P
{
    /// <summary>
    ///     Peer settings class.
    /// </summary>
    public sealed class PeerSettings : IPeerSettings
    {
        private readonly NetworkType _networkType;
        public NetworkType NetworkType => _networkType;
        public string PublicKey { get; }
        public int Port { get; }
        public string PayoutAddress { get; }
        public IPAddress BindAddress { get; private set; }
        public IList<string> SeedServers { get; }
        public IPEndPoint[] DnsServers { get; }
        public MultiAddress Address { set; get; }
        
        public IPAddress PublicIpAddress { private set; get; }

        private readonly IAddressProvider _addressProvider;

        public IEnumerable<MultiAddress> GetAddresses(MultiAddress address, Peer localPeer)
        {
            var result = new MultiAddress($"{address}/ipfs/{localPeer.Id}");

            // Get the actual IP address(es).
            IList<MultiAddress> addresses = null;
            var ips = NetworkInterface.GetAllNetworkInterfaces()

               // It appears that the loopback adapter is not UP on Ubuntu 14.04.5 LTS
               .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                 || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
               .SelectMany(nic => nic.GetIPProperties().UnicastAddresses);
            if (result.ToString().StartsWith("/ip4/0.0.0.0/"))
            {
                addresses = ips
                   .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork)
                   .Select(ip => new MultiAddress(result.ToString().Replace("0.0.0.0", ip.Address.ToString())))
                   .ToList();
            }
            else if (result.ToString().StartsWith("/ip6/::/"))
            {
                addresses = ips
                   .Where(ip => ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                   .Select(ip => { return new MultiAddress(result.ToString().Replace("::", ip.Address.ToString())); })
                   .ToList();
            }
            else
            {
                addresses = new[] { result };
            }

            if (!addresses.Any())
            {
                var msg = "Cannot determine address(es) for " + result;

                foreach (var ip in ips)
                {
                    msg += " nic-ip: " + ip.Address;
                }

                throw new Exception(msg);
            }

            return addresses;
        }

        /// <summary>
        ///     Set the local nodes peer settings
        /// </summary>
        /// <param name="rootSection"></param>
        public PeerSettings(IConfigurationRoot rootSection, Peer localPeer, IDfsConfigApi dfsConfigApi, IAddressProvider addressProvider)
        {
            Guard.Argument(rootSection, nameof(rootSection)).NotNull();
            _addressProvider = addressProvider;

            var section = rootSection.GetSection("CatalystNodeConfiguration").GetSection("Peer");
            Enum.TryParse(section.GetSection("Network").Value, out _networkType);
            BindAddress = IPAddress.Parse(section.GetSection("BindAddress").Value);
            var publicIpAddress = IPAddress.Parse(section.GetSection("PublicIpAddress").Value);

            UpdateNetworkConfig(_networkType).GetAwaiter().GetResult();

            var pksi = Convert.FromBase64String(localPeer.PublicKey);
            PublicKey = pksi.GetPublicKeyBytesFromPeerId().ToBase58();

            Port = int.Parse(section.GetSection("Port").Value);
            PayoutAddress = section.GetSection("PayoutAddress").Value;
            
            SeedServers = section.GetSection("SeedServers").GetChildren().Select(p => p.Value).ToList();
            DnsServers = section.GetSection("DnsServers")
               .GetChildren()
               .Select(p => EndpointBuilder.BuildNewEndPoint(p.Value)).ToArray();

            

            var json = dfsConfigApi.GetAsync("Addresses.Swarm").ConfigureAwait(false).GetAwaiter().GetResult();
            
            List<MultiAddress> addresses = new List<MultiAddress>();
            foreach (string a in json)
            {
                addresses.AddRange(GetAddresses(a, localPeer));
            }

            Address = addresses.First();
        }

        private async Task UpdateNetworkConfig(NetworkType networkType)
        {
            var publicIp = _addressProvider.GetPublicIpAsync();
            var localIp = _addressProvider.GetLocalIpAsync();

            await Task.WhenAll(new[]{publicIp, localIp});
            var configApi = new NetworkConfigApi(networkType);

            if (localIp.IsCompletedSuccessfully && localIp.Result!=null && !localIp.Result.Equals(BindAddress))
            {
                BindAddress = localIp.Result;
                await configApi.SetAsync("CatalystNodeConfiguration.Peer.BindAddress", BindAddress.ToString());
                await configApi.SetAsync("CatalystNodeConfiguration.Rpc.BindAddress", BindAddress.ToString());
            }
            
            if (publicIp.IsCompletedSuccessfully && publicIp.Result!=null && !publicIp.Result.Equals(BindAddress))
            {
                PublicIpAddress = publicIp.Result;
                await configApi.SetAsync("CatalystNodeConfiguration.Peer.PublicIpAddress", PublicIpAddress.ToString());
            }
        }
    }
}
