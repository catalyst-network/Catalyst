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
using Autofac;
using Catalyst.Abstractions.Hashing;
using FluentAssertions;
using Ipfs.Registry;
using Xunit;

namespace Catalyst.Core.Modules.Hashing.Tests.UnitTests
{
    public class HashingTests
    {
        private readonly IContainer _container;

        public HashingTests()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<HashingModule>();

            _container = builder.Build();
            _container.BeginLifetimeScope();
        }

        [Fact]
        public void HashProvider_Can_Be_Resolved() { _container.Resolve<IHashProvider>().Should().NotBeNull(); }

        [Fact]
        public void MultihashAlgorithm_Can_Be_Resolved()
        {
            _container.Resolve<HashingAlgorithm>().Should().NotBeNull();
        }

        [Fact]
        public void Can_Hash_Data()
        {
            var hashProvider = _container.Resolve<IHashProvider>();
            var data = BitConverter.GetBytes(0xDEADBEEF);
            var multiHash = hashProvider.ComputeMultiHash(data);
            multiHash.Should().NotBeNull();
        }
    }
}
