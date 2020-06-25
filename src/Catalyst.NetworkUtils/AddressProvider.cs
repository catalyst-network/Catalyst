using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Modules.UPnP;
using Serilog;
using Serilog.Core;

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
        
        public async Task<IPAddress> GetPublicIpAsync()
        {
            var ipAddress = await _uPnPUtility.GetPublicIpAddress(2);

            if(ipAddress!=null)
            {
                return ipAddress;
            }

            return await GetPublicIpFromWebAsync();
        }
        
        //Doesn't matter if specified host is reachable, as no real connection needs to be established
        public async Task<IPAddress> GetLocalIpAsync()
        {
            using var socket = _socketFactory.Create(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            await socket.ConnectAsync("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address;
        }

        private async Task<IPAddress> GetPublicIpFromWebAsync()
        {
            var urls = new[] {"https://ip.catalyst.workers.dev", "http://icanhazip.com"};

            foreach(var url in urls)
            {
                var ipAddress = await _webClientFactory.Create().DownloadStringTaskAsync(url);
                if(IPAddress.TryParse(ipAddress, out var validIpAddress))
                {
                    return validIpAddress;
                }
            }

            return null;
        }
    }
}
