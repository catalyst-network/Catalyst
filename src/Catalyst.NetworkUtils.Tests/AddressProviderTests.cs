using System;
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
        private readonly string ipAddress = "192.168.0.12";


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
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs(IPAddress.Parse(ipAddress));
            
            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(ipAddress));
            _webClient1.Received(0).DownloadStringTaskAsync(default);
            _webClient2.Received(0).DownloadStringTaskAsync(default);
        }
        [Test]
        public void Can_Return_Public_IpAddress_From_WebClient_On_UPnP_Timeout()
        {
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs((IPAddress)null);
            _webClient1.DownloadStringTaskAsync(default).ReturnsForAnyArgs(ipAddress);
            _webClient2.DownloadStringTaskAsync(default).ReturnsForAnyArgs(ipAddress);

            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(ipAddress));
            _webClient1.ReceivedWithAnyArgs().DownloadStringTaskAsync(default);
        }
        
        [Test]
        public void Can_Return_Public_IpAddress_Where_Not_All_WebClients_Return_Address()
        {
            _uPnPUtility.GetPublicIpAddress().ReturnsForAnyArgs((IPAddress)null);
            _webClient2.DownloadStringTaskAsync(default).ReturnsForAnyArgs(ipAddress);
            _webClient1.ClearSubstitute();

            _addressProvider.GetPublicIpAsync().Result.Should().Be(IPAddress.Parse(ipAddress));
            _webClient2.ReceivedWithAnyArgs().DownloadStringTaskAsync(default);
        }

        [Test]
        public void Can_Return_LocalIp_Address()
        {
            _socket.ConnectAsync(default, default).ReturnsForAnyArgs(Task.CompletedTask);
            _socket.LocalEndPoint.Returns(IPEndPoint.Parse(ipAddress));

            _addressProvider.GetLocalIpAsync().Result.Should().Be(IPAddress.Parse(ipAddress));
        }

    }
}
