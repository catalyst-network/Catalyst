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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Modules.UPnP;
using Nito.AsyncEx;

namespace Catalyst.NetworkUtils
{
    public class AddressProvider : IAddressProvider
    {
        private readonly IUPnPUtility _uPnPUtility;
        private readonly IWebClientFactory _webClientFactory;
        private readonly ISocketFactory _socketFactory;

        public AddressProvider(IUPnPUtility uPnPUtility, IWebClientFactory webClientFactory, ISocketFactory socketFactory)
        {
            _uPnPUtility = uPnPUtility;
            _webClientFactory = webClientFactory;
            _socketFactory = socketFactory;
        }
        
        public async Task<IPAddress> GetPublicIpAsync(CancellationToken cancel = default)
        {
            var ipAddress = await _uPnPUtility.GetPublicIpAddress(cancel).ConfigureAwait(false);

            if(ipAddress!=null)
            {
                return ipAddress;
            }

            return await GetPublicIpFromWebAsync(cancel);
        }
        
        //Doesn't matter if specified host is reachable, as no real connection needs to be established
        public async Task<IPAddress> GetLocalIpAsync(CancellationToken cancel = default)
        {
            if (cancel.IsCancellationRequested) return null;
            using var socket = _socketFactory.Create(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            
            await socket.ConnectAsync("8.8.8.8", 65530).ConfigureAwait(false);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address;

        }

        private async Task<IPAddress> GetPublicIpFromWebAsync(CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested) return null;
            var urls = new[] {"https://ip.catalyst.workers.dev", "http://icanhazip.com"};

            foreach(var url in urls)
            {
                var ipAddress = await _webClientFactory.Create().DownloadStringTaskAsync(url).ConfigureAwait(false);
                if(IPAddress.TryParse(ipAddress, out var validIpAddress))
                {
                    return validIpAddress;
                }
            }

            return null;
        }
    }
}
