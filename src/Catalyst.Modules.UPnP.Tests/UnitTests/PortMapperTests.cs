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
using System.Threading.Tasks;
using Mono.Nat;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using Serilog;


namespace Catalyst.Modules.UPnP.Tests.UnitTests
{
    public class PortMapperTests
    {
       
        private PortMapper _portMapper;
        private INatDevice _device = Substitute.For<INatDevice>();
        private INatUtilityProvider _natUtilityProvider = Substitute.For<INatUtilityProvider>();
        private ILogger _logger;
        private Mapping _mappingA;
        private Mapping _mappingB;
        private Mapping _mappingC;


        [SetUp]
        public void Init()
        {
             var portA = 6024;
             var portB = 6025;
            _mappingA = new Mapping(Mono.Nat.Protocol.Tcp, portA, portA);
            _mappingB = new Mapping(Mono.Nat.Protocol.Tcp, portB, portB);
            _mappingC = new Mapping(Mono.Nat.Protocol.Tcp, portA, portB);
            _logger = Substitute.For<ILogger>();
            
            
        }

        [Test]
        public async Task PortMapper_Stops_Searching_After_Timeout()
        {
            const int secondsTimeout = 5;
            var portMapper = new PortMapper(_natUtilityProvider, _logger);
            portMapper.AddPortMappings(new List<Mapping>(), secondsTimeout);
            await Task.Delay(TimeSpan.FromSeconds(secondsTimeout + 1));
            _natUtilityProvider.Received(1).StartDiscovery();
            _natUtilityProvider.Received(1).StopDiscovery();
        }
        
        [Test]
        public async Task PortMapper_Stops_Searching_After_Task_Finished()
        {
            const int secondsTimeout = 500;
            var natUtilityProvider = new TestNatUtilityProvider(_device);
            var portMapper = new PortMapper(natUtilityProvider, _logger);
            portMapper.AddPortMappings(new List<Mapping>(), secondsTimeout);
            await Task.Delay(TimeSpan.FromSeconds(10));
            _natUtilityProvider.Received(1).StartDiscovery();
            _natUtilityProvider.Received(1).StopDiscovery();
        }
        
        [Test]
        public async Task On_Device_Found_Adds_Mapping_If_Mapping_Doesnt_Exist()
        {
            var secondsTimeout = 5;
            
            Mapping[] existingMappings = {_mappingA};
            Mapping[] attemptedMappings = {_mappingB};
            
            var device = Substitute.For<INatDevice>();
            device.GetAllMappingsAsync()
                .Returns(Task.FromResult(existingMappings));
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.AddPortMappings(attemptedMappings, secondsTimeout);
            await device.Received(1).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public async Task On_Device_Found_Does_Not_Add_Mapping_If_Mapping_Exists()
        {
            var secondsTimeout = 5;
            
            Mapping[] existingMappings = {_mappingA, _mappingB};
            Mapping[] attemptedMappings = {_mappingA};
            var device = Substitute.For<INatDevice>();
            device.GetAllMappingsAsync()
                .Returns(Task.FromResult(existingMappings));
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            await portMapper.AddPortMappings(attemptedMappings, secondsTimeout);

            await device.Received(0).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public async Task On_Device_Found_Does_Not_Add_Mapping_If_Mapping_Partially_Exists()
        {
            var secondsTimeout = 5;
            Mapping[] existingMappings = {_mappingA};
            Mapping[] attemptedMappings = {_mappingC};  //mapping C has one port shared with mapping A
            var device = Substitute.For<INatDevice>();
            device.GetAllMappingsAsync()
                .Returns(Task.FromResult(existingMappings));
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            await portMapper.AddPortMappings(attemptedMappings, secondsTimeout);

            await device.Received(0).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public void Can_Remove_Mapped_Port()
        {
           // _portMapper.MapPort(_device, _port);
        }


        private class TestNatUtilityProvider : INatUtilityProvider
        {
            private readonly INatDevice _device;
            public TestNatUtilityProvider(INatDevice device)
            {
                _device = device;
            }
            public event EventHandler<DeviceEventArgs> DeviceFound;
            public void StartDiscovery()
            {
                DeviceFound?.Invoke(this, new DeviceEventArgs(_device));
            }

            public void StopDiscovery()
            {
            }
        }
        
    }
}
