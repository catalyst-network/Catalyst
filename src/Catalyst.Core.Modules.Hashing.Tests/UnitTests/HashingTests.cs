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

using System;
using System.Linq;
using Autofac;
using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Modules.Hashing;
using Catalyst.Protocol.Transaction;
using FluentAssertions;
using MultiFormats.Registry;
using Google.Protobuf;
using NUnit.Framework;

namespace Catalyst.Core.Modules.Consensus.Tests.UnitTests
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

        [Test]
        public void HashProvider_Can_Be_Resolved() { _container.Resolve<IHashProvider>().Should().NotBeNull(); }

        [Test]
        public void MultihashAlgorithm_Can_Be_Resolved()
        {
            _container.Resolve<HashingAlgorithm>().Should().NotBeNull();
        }

        [Test]
        public void Can_Hash_Data()
        {
            var hashProvider = _container.Resolve<IHashProvider>();
            var data = BitConverter.GetBytes(0xDEADBEEF);
            var multiHash = hashProvider.ComputeMultiHash(data);
            multiHash.Should().NotBeNull();
        }

        [Test]
        public void Hashes_messages()
        {
            var hashProvider = _container.Resolve<IHashProvider>();
            var entry = new PublicEntry();

            var arrayHash = hashProvider.ComputeMultiHash(entry.ToByteArray());
            var messageHash = hashProvider.ComputeMultiHash(entry);

            arrayHash.ToArray().Should().BeEquivalentTo(messageHash.ToArray());
        }

        [Test]
        public void Hashes_messages_with_suffix()
        {
            var hashProvider = _container.Resolve<IHashProvider>();
            var entry = new PublicEntry();

            var suffix = new byte[1];

            var arrayHash = hashProvider.ComputeMultiHash(entry.ToByteArray().Concat(suffix).ToArray());
            var messageHash = hashProvider.ComputeMultiHash(entry, suffix);

            arrayHash.ToArray().Should().BeEquivalentTo(messageHash.ToArray());
        }
    }
}
