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

using System.Threading.Tasks;
using Catalyst.TestUtils;
using Mono.Nat;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Catalyst.UPnP.Tests.Utils;
using FluentAssertions;

namespace Catalyst.Modules.AutoPortMapper.IntegrationTests
{
    [TestFixture]
    [Category(Traits.IntegrationTest)] 
    public class AutoPortMapperTests
    {
        private ILogger _logger;
        
        [SetUp]
        public void Init()
        {
            _logger = Substitute.For<ILogger>();
        }
        
        [Test]
        public async Task Can_Add_Ports_From_Devnet_File()
        {
            Mapping[] existingMappings = {};
            var device = new TestNatDeviceWithInternalMappings(existingMappings);
            
            device.GetAllMappingsAsync().Result.Should().HaveCount(0);
            
            await Program.Start(new [] {"--filepath", "./Config/devnet.json"}, _logger, new TestNatUtilityProvider(device));
           
            device.GetAllMappingsAsync().Result.Should().HaveCount(2);
        }

        [Test]
        public async Task Can_Add_Ports_From_Devnet_File_And_Delete()
        {
            Mapping[] existingMappings = {};
            var device = new TestNatDeviceWithInternalMappings(existingMappings);
            
            device.GetAllMappingsAsync().Result.Should().HaveCount(0);
            
            await Program.Start(new string[] {"--filepath", "./Config/devnet.json"}, _logger, new TestNatUtilityProvider(device));
           
            device.GetAllMappingsAsync().Result.Should().HaveCount(2);
           
           await Program.Start(new string[] {"--filepath", "./Config/devnet.json", "--delete"}, _logger, new TestNatUtilityProvider(device));
           
           device.GetAllMappingsAsync().Result.Should().HaveCount(0);
        }
    }
}

