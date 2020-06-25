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

using FluentAssertions;
using Mono.Nat;
using NUnit.Framework;

namespace Catalyst.Modules.AutoPortMapper.UnitTests
{
    public class PortMappingParserTests
    {

        [Test]
        public void Can_Parse_Using_Default_Identifiers()
        {
            var json = @"{'CatalystNodeConfiguration': { 'Peer': {'Port': 42076},'Rpc': {'Port': 42066}}}";
            var mappings = PortMappingParser.ParseJson(PortMappingConstants.DefaultTcpProperty, PortMappingConstants.DefaultUdpProperty,
                json);
            mappings.Should().Contain(new Mapping(Mono.Nat.Protocol.Tcp, 42076, 42076));
            mappings.Should().Contain(new Mapping(Mono.Nat.Protocol.Udp, 42066, 42066));
        }
        
        [Test]
        public void Can_Parse_With_Non_Default_Identifiers()
        {
            var json = @"{'Peer': { 'Port': {'Port1': 42076}}}";
            var mappings = PortMappingParser.ParseJson("Peer.Port.Port1", PortMappingConstants.DefaultUdpProperty,
                json);
            mappings.Should().Contain(new Mapping(Mono.Nat.Protocol.Tcp, 42076, 42076));
        }
        
        [Test]
        public void Can_Parse_With_Multiple_Comma_Separated_Identifiers()
        {
            var json = @"{'Peer': { 'Port': {'Port1': 42076, 'Port2': 27676}}}";
            var mappings = PortMappingParser.ParseJson("Peer.Port.Port1,Peer.Port.Port2", PortMappingConstants.DefaultUdpProperty,
                json);
            mappings.Should().Contain(new Mapping(Mono.Nat.Protocol.Tcp, 42076, 42076));
            mappings.Should().Contain(new Mapping(Mono.Nat.Protocol.Tcp, 27676, 27676));
        }
        
        [Test]
        public void When_Identifiers_Do_Not_Exist_In_Json_Mapping_Is_Empty()
        {
            var json = @"{}";
            var mappings = PortMappingParser.ParseJson(PortMappingConstants.DefaultTcpProperty, PortMappingConstants.DefaultUdpProperty,
                json);
            mappings.Should().HaveCount(0);
        }
    }
    
 
}
