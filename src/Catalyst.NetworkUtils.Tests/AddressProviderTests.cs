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

using System.Net;
using System.Threading.Tasks;
using Catalyst.Modules.UPnP;
using Catalyst.NetworkUtils;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;

namespace Catalyst.Modules.NetworkUtils.Tests
{
    public class AddressProviderTests
    {
        private readonly IUPnPUtility _uPnPUtility = Substitute.For<IUPnPUtility>();
        private readonly IWebClientFactory _webClientFactory = Substitute.For<IWebClientFactory>();
        private readonly IWebClient _webClient1 = Substitute.For<IWebClient>();
        private readonly IWebClient _webClient2 = Substitute.For<IWebClient>();
        private readonly ISocketFactory _socketFactory = Substitute.For<ISocketFactory>();
        private readonly ISocket _socket = Substitute.For<ISocket>();
        private IAddressProvider _addressProvider;
        private readonly string _localAddress = "192.168.0.10";
        private readonly string _publicAddress = "55.55.555.55";
        


        [SetUp]
        public void Init()
        {
            _addressProvider = new AddressProvider(_uPnPUtility, _webClientFactory, _socketFactory);
            _webClientFactory.Create().Returns(_webClient1, _webClient2);
            _socketFactory.Create(default, default, default).ReturnsForAnyArgs(_socket);
        }

        [Test]
        public void Does_Not_Use_Web_If_UPnP_Returns_Public_IpAddress()
        {
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs(IPAddress.Parse(_publicAddress));
            
            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(_publicAddress));
            _webClient1.Received(0).DownloadStringTaskAsync(default);
            _webClient2.Received(0).DownloadStringTaskAsync(default);
        }
        [Test]
        public void Can_Return_Public_IpAddress_From_WebClient_On_UPnP_Timeout()
        {
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs((IPAddress)null);
            _webClient1.DownloadStringTaskAsync(default).ReturnsForAnyArgs(_publicAddress);
            _webClient2.DownloadStringTaskAsync(default).ReturnsForAnyArgs(_publicAddress);

            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(_publicAddress));
            _webClient1.ReceivedWithAnyArgs().DownloadStringTaskAsync(default);
        }
        
        [Test]
        public void Can_Return_Public_IpAddress_Where_Not_All_WebClients_Return_Address()
        {
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs((IPAddress)null);
            _webClient2.DownloadStringTaskAsync(default).ReturnsForAnyArgs(_publicAddress);
            _webClient1.ClearSubstitute();

            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(_publicAddress));
            _webClient2.ReceivedWithAnyArgs().DownloadStringTaskAsync(default);
        }

        [Test]
        public void Can_Return_LocalIp_Address()
        {
            _socket.ConnectAsync(default, default).ReturnsForAnyArgs(Task.CompletedTask);
            _socket.LocalEndPoint.Returns(IPEndPoint.Parse(_localAddress));

            _addressProvider.GetLocalIpAsync().Result.Should().Be(IPAddress.Parse(_localAddress));
        }

    }
}
