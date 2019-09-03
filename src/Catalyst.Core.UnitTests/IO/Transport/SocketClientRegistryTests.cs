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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Catalyst.Abstractions.IO.Transport;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Rpc;
using Catalyst.Core.IO.Events;
using Catalyst.Core.IO.Transport;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.UnitTests.IO.Transport
{
    public sealed class SocketClientRegistryTests
    {
        private static ISocketClient GetSubstituteForActiveSocketClient()
        {
            var substitute = Substitute.For<ISocketClient>();
            substitute.Channel.Active.Returns(true);
            return substitute;
        }

        private Dictionary<int, ISocketClient> ConstructSocketByEndpointSampleData()
        {
            return new[] {2000, 3000, 4000, 5000, 6000}.ToDictionary(
                p => new IPEndPoint(IPAddress.Loopback, p).GetHashCode(),
                p => GetSubstituteForActiveSocketClient()
            );
        }

        private ISocketClientRegistry<ISocketClient> ConstructPopulatedSocketRepository(Dictionary<int, ISocketClient> socketsByEndpointHashCode)
        {
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>();
            socketsByEndpointHashCode.ToList().ForEach(element =>
                clientSocketRegistry.AddClientToRegistry(element.Key, element.Value)
            );
            return clientSocketRegistry;
        }

        [Fact]
        public void Can_Add_Multiple_Sockets_To_Registry()
        {
            var socketsByEndpointHashCode = ConstructSocketByEndpointSampleData();
            var clientSocketRegistry = ConstructPopulatedSocketRepository(socketsByEndpointHashCode);
            clientSocketRegistry.Registry
               .Should().NotBeEmpty()
               .And
               .HaveCount(5)
               .And.ContainValues(socketsByEndpointHashCode.Values);
        }

        [Fact]
        public void Can_Add_Multiple_Sockets_To_Registry_And_Get_One()
        {
            var socketsByEndpointHashCode = ConstructSocketByEndpointSampleData();
            var clientSocketRegistry = ConstructPopulatedSocketRepository(socketsByEndpointHashCode);

            var socketClient =
                clientSocketRegistry.GetClientFromRegistry(new IPEndPoint(IPAddress.Loopback, 2000).GetHashCode());
            socketClient.Should()
               .NotBeNull()
               .And
               .BeAssignableTo<ISocketClient>();
        }

        [Fact]
        public void Can_Add_Multiple_Sockets_To_Registry_And_Remove_One()
        {
            var socketsByEndpointHashCode = ConstructSocketByEndpointSampleData();
            var clientSocketRegistry = ConstructPopulatedSocketRepository(socketsByEndpointHashCode);

            socketsByEndpointHashCode.Remove(new IPEndPoint(IPAddress.Loopback, 2000).GetHashCode(), out _);

            var socketClient =
                clientSocketRegistry.RemoveClientFromRegistry(new IPEndPoint(IPAddress.Loopback, 2000).GetHashCode());
            socketClient.Should().BeTrue();

            clientSocketRegistry.Registry
               .Should().NotBeEmpty()
               .And
               .HaveCount(4)
               .And.ContainValues(socketsByEndpointHashCode.Values);
        }

        [Fact]
        public void Can_Add_Socket_To_Registry_And_Get_Same_Client_From_HashCode()
        {
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>();
            var socket = GetSubstituteForActiveSocketClient();
            var hashcode = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort).GetHashCode();

            clientSocketRegistry.AddClientToRegistry(hashcode, socket);

            clientSocketRegistry.Registry
               .Should().ContainKey(hashcode);

            clientSocketRegistry.Registry
               .Should().ContainValue(socket);

            clientSocketRegistry.GetClientFromRegistry(hashcode)
               .Should().NotBeNull()
               .And
               .BeAssignableTo<ISocketClient>();
        }

        [Fact]
        public void Can_init_peer_client_registry()
        {
            var socketRegistry = new SocketClientRegistry<IPeerClient>();
            Assert.Equal(socketRegistry.GetRegistryType(), typeof(IPeerClient).Name);
        }

        [Fact]
        public void Can_init_rcp_client_registry()
        {
            var socketRegistry = new SocketClientRegistry<INodeRpcClient>();
            Assert.Equal(socketRegistry.GetRegistryType(), typeof(INodeRpcClient).Name);
        }

        [Fact]
        public void Can_init_tcp_client_registry()
        {
            var socketRegistry = new SocketClientRegistry<ITcpClient>();
            Assert.Equal(socketRegistry.GetRegistryType(), typeof(ITcpClient).Name);
        }

        [Fact]
        public void Can_init_udp_client_registry()
        {
            var socketRegistry = new SocketClientRegistry<IUdpClient>();
            Assert.Equal(socketRegistry.GetRegistryType(), typeof(IUdpClient).Name);
        }

        [Fact]
        public void Can_Listen_To_Registry_Client_Added_Events()
        {
            var testScheduler = new TestScheduler();
            var socketsByEndpointHashCode = ConstructSocketByEndpointSampleData();
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>(testScheduler);

            var connectionEvents = new List<int>();
            clientSocketRegistry.EventStream.OfType<SocketClientRegistryClientAdded>().Subscribe(
                socketClientRegistryClientAdded =>
                {
                    connectionEvents.Add(socketClientRegistryClientAdded.SocketHashCode);
                });

            socketsByEndpointHashCode.ToList().ForEach(element =>
                clientSocketRegistry.AddClientToRegistry(element.Key, element.Value)
            );

            testScheduler.Start();

            connectionEvents.Should().NotBeEmpty().And.HaveCount(5);
        }

        [Fact]
        public void Can_Listen_To_Registry_Client_Removed_Events()
        {
            var testScheduler = new TestScheduler();
            var socketsByEndpointHashCode = ConstructSocketByEndpointSampleData();
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>(testScheduler);

            var connectionEvents = new List<int>();
            clientSocketRegistry.EventStream.OfType<SocketClientRegistryClientAdded>().Subscribe(
                socketClientRegistryClientAdded =>
                    connectionEvents.Add(socketClientRegistryClientAdded.SocketHashCode)
            );

            clientSocketRegistry.EventStream.OfType<SocketClientRegistryClientRemoved>().Subscribe(
                socketClientRegistryClientAddedRemoved =>
                    connectionEvents.Remove(socketClientRegistryClientAddedRemoved.SocketHashCode)
            );

            socketsByEndpointHashCode.ToList().ForEach(element =>
                clientSocketRegistry.RemoveClientFromRegistry(element.Key)
            );

            testScheduler.Start();

            connectionEvents.Should().BeEmpty();
        }

        [Fact]
        public void Can_Remove_Socket_From_Registry()
        {
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>();
            var subscribedSocket = GetSubstituteForActiveSocketClient();
            var hashcode = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort).GetHashCode();
            clientSocketRegistry.AddClientToRegistry(hashcode, subscribedSocket);

            clientSocketRegistry.Registry
               .Should().ContainKey(hashcode);

            clientSocketRegistry.Registry
               .Should().ContainValue(subscribedSocket);

            clientSocketRegistry.RemoveClientFromRegistry(hashcode);

            clientSocketRegistry.Registry
               .Should().NotContainKey(hashcode);

            clientSocketRegistry.Registry
               .Should().NotContainValue(subscribedSocket);

            clientSocketRegistry.Registry
               .Should().BeEmpty();
        }

        [Fact]
        public void Cannot_Add_Inactive_Client()
        {
            var clientSocketRegistry = new SocketClientRegistry<ISocketClient>();
            var socket = Substitute.For<ISocketClient>();
            socket.Channel.Active.Returns(false);
            var hashcode = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort).GetHashCode();

            new Action(() => clientSocketRegistry.AddClientToRegistry(hashcode, socket)).Should()
               .Throw<ArgumentException>();
        }

        [Fact]
        public void Socket_Registry_Has_A_List()
        {
            new SocketClientRegistry<ISocketClient>().Registry
               .Should().NotBeNull()
               .And
               .BeEmpty()
               .And
               .BeOfType(typeof(ConcurrentDictionary<int, ISocketClient>));
        }
    }
}
