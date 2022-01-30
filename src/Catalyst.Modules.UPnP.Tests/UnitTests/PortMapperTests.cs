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

using System.Threading;
using System.Threading.Tasks;
using Catalyst.UPnP.Tests.Utils;
using FluentAssertions;
using Mono.Nat;
using NSubstitute;
using NUnit.Framework;
using Serilog;

namespace Catalyst.Modules.UPnP.Tests.UnitTests
{
    public class PortMapperTests
    {
        private const int SecondsTimeout = 5;
        private readonly ILogger _logger = Substitute.For<ILogger>();
        private Mapping _mappingA;
        private Mapping _mappingB;
        private Mapping _mappingC;

        [SetUp]
        public void Init()
        {
             const int portA = 6024;
             const int portB = 6025;
            _mappingA = new Mapping(Mono.Nat.Protocol.Tcp, portA, portA);
            _mappingB = new Mapping(Mono.Nat.Protocol.Tcp, portB, portB);
            _mappingC = new Mapping(Mono.Nat.Protocol.Tcp, portB, portA);
        }

        [Test]
        public async Task PortMapper_Stops_Searching_After_Timeout()
        {
            var natUtilityProvider = Substitute.For<INatUtilityProvider>();
            var portMapper = new PortMapper(natUtilityProvider, _logger);
            var outcome = await portMapper.MapPorts(new Mapping[]{}, new CancellationTokenSource(), SecondsTimeout);
            outcome.Should().Be(PortMapperConstants.Result.Timeout);
            natUtilityProvider.Received(1).StartDiscovery();
            natUtilityProvider.Received(1).StopDiscovery();
        }
        
        [Test]
        public async Task PortMapper_Stops_Searching_After_Task_Finished()
        {
            var device = Substitute.For<INatDevice>();
            var natUtilityProvider = new TestNatUtilityProvider(device);
            var portMapper = new PortMapper(natUtilityProvider, _logger);
            var outcome = await portMapper.MapPorts(new Mapping[]{}, new CancellationTokenSource(), SecondsTimeout);
            outcome.Should().Be(PortMapperConstants.Result.TaskFinished);
        }

        [Test]
        public async Task Adds_Mapping_If_Mapping_Doesnt_Exist()
        {
            Mapping[] existingMappings = {_mappingA};
            Mapping[] attemptedMappings = {_mappingB};
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.MapPorts(attemptedMappings, new CancellationTokenSource(), SecondsTimeout);
            await device.Received(1).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public async Task Can_Add_Multiple_Mappings()
        {
            Mapping[] existingMappings = {};
            Mapping[] attemptedMappings = {_mappingA,_mappingB};
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);

            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.MapPorts(attemptedMappings, new CancellationTokenSource(), SecondsTimeout);
            await device.Received(1).CreatePortMapAsync(_mappingA);
            await device.Received(1).CreatePortMapAsync(_mappingB);
        }
        
        [Test]
        public async Task Does_Not_Add_Mapping_If_Mapping_Exists()
        {

            Mapping[] existingMappings = {_mappingA, _mappingB};
            Mapping[] attemptedMappings = {_mappingA};
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            await portMapper.MapPorts(attemptedMappings, new CancellationTokenSource(), SecondsTimeout);

            await device.Received(0).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public async Task Does_Not_Add_Mapping_If_Mapping_Partially_Exists()
        {
            Mapping[] existingMappings = {_mappingA};
            Mapping[] attemptedMappings = {_mappingC};  //mapping C has one port shared with mapping A
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            await portMapper.MapPorts(attemptedMappings, new CancellationTokenSource(), SecondsTimeout);

            await device.Received(0).CreatePortMapAsync(Arg.Is(attemptedMappings[0]));
        }
        
        [Test]
        public async Task If_Alternative_Mapping_Added_By_Device_It_Is_Subsequently_Deleted()
        {
            Mapping[] existingMappings = {};
            Mapping[] attemptedMappings = {_mappingA};

            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            device.CreatePortMapAsync(_mappingA)
                .Returns(Task.FromResult(_mappingB));

            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.MapPorts(attemptedMappings, new CancellationTokenSource(), SecondsTimeout);
            await device.Received(1).DeletePortMapAsync(_mappingB);
        }
        
        [Test]
        public async Task Can_Remove_Mapped_Port_If_Mapping_Exists()
        {
            Mapping[] existingMappings = {_mappingA};
            Mapping[] mappingsToDelete = {_mappingA};
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.MapPorts(mappingsToDelete, new CancellationTokenSource(), SecondsTimeout, true);
            await device.Received(1).DeletePortMapAsync(Arg.Is(_mappingA));
        }
        
        [Test]
        public async Task Does_Not_Attempt_Remove_Mapped_Port_If_Mapping_Doesnt_Exist()
        {
            Mapping[] existingMappings = {_mappingA};
            Mapping[] mappingsToDelete = {_mappingB};
            
            var device = Utils.GetTestDeviceWithExistingMappings(existingMappings);
            
            var portMapper = new PortMapper(new TestNatUtilityProvider(device), _logger);
            
            await portMapper.MapPorts(mappingsToDelete, new CancellationTokenSource(), SecondsTimeout, true);
            await device.Received(0).DeletePortMapAsync(Arg.Is(_mappingB));
        }
    }
}
