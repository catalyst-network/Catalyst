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
using System.Linq;
using Autofac;
using Catalyst.Abstractions.Hashing;
using Catalyst.Modules.UPnP;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using Mono.Nat;
using MultiFormats.Registry;

using Xunit;

namespace Catalyst.Modules.UPnP.Tests.UnitTests
{
    public class PMPTests
    {
       /* private readonly IContainer _container;
        private readonly PortMapper _portMapper;
        private readonly INatDevice _device;
        private readonly int _port;

        public PMPTests()
        {
            //var builder = new ContainerBuilder();
            //builder.RegisterModule<HashingModule>();

            //_container = builder.Build();
            // _container.BeginLifetimeScope();
            _device = NSubstitute.Substitute.For<INatDevice>();
            _portMapper = new PortMapper();
        }

        [Fact]
        public void Can_Discover_Device()
        {
            _portMapper.Discover().Should.Return(Type(INatDevice));
        }
        
        [Fact]
        public void Can_Map_Port()
        {
            _portMapper.MapPort(_device, _port);
        }
        
        [Fact]
        public void Can_Remove_Mapped_Port()
        {
            _portMapper.MapPort(_device, _port);
        }
        
        [Fact]
        public void Can_Add_Mapping_From_File()
        {
            //_portMapper.MapPort(_config, _port);
        }
        */
    }
}
