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
using Catalyst.Protocol.Common;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit;

namespace Catalyst.Protocol.UnitTests.Shared
{
    public class AddressTests
    {
        private readonly byte[] _noPrefixBytes;

        public AddressTests()
        {
            var random = new Random();
            _noPrefixBytes = new byte[18];
            random.NextBytes(_noPrefixBytes);
        }

        [Fact]
        public void Address_Constructor_From_Raw_Bytes_Should_Throw_On_Null_Argument()
        {
            new Action(() => new Address(null)).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Address_Constructor_From_Raw_Bytes_Should_Throw_On_Bad_Network()
        {
            var wrongNetwork = (byte) 255;
            var isSmartContract = (byte) 1;
            var fullAddress = new[] {wrongNetwork, isSmartContract}.Concat(_noPrefixBytes).ToArray();

            new Action(() => new Address(fullAddress)).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Address_Constructor_From_Raw_Bytes_Should_Throw_On_Bad_SmartContract_Byte()
        {
            var network = (byte) 1;
            var isSmartContract = (byte) 8;
            var fullAddress = new[] {network, isSmartContract}.Concat(_noPrefixBytes).ToArray();

            new Action(() => new Address(fullAddress)).Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Address_Constructor_From_Raw_Bytes_Should_Throw_On_Bad_Byte_Length()
        {
            var network = (byte) 1;
            var isSmartContract = (byte) 8;
            var fullAddress = new[] {network, isSmartContract, (byte) 123}.Concat(_noPrefixBytes).ToArray();

            fullAddress.Length.Should()
               .BeGreaterThan(Address.ByteLength, "otherwise the test is not useful");

            new Action(() => new Address(fullAddress)).Should().Throw<ArgumentException>();
        }
        
        [Fact]
        public void Address_Constructor_From_Raw_Bytes_Should_Work_On_Correct_Bytes()
        {
            var network = (byte) (int) Network.Devnet;
            var isSmartContract = (byte) 1;
            var fullAddress = new[] {network, isSmartContract}.Concat(_noPrefixBytes).ToArray();

            var address = new Address(fullAddress);
            address.Network.Should().Be(Network.Devnet);
            address.IsSmartContract.Should().Be(true);
            address.RawBytes.Should().EndWith(_noPrefixBytes);
        }

        [Fact]
        public void Address_Constructor_From_IPublicKey_Should_Fail_On_Null_Public_Key()
        {
            new Action(() => new Address(null,
                    Network.Devnet, 
                    Substitute.For<IMultihashAlgorithm>(), 
                    false))
               .Should().Throw<ArgumentException>();
        }

        [Fact(Skip = "placeholder until the common lib comes in")]
        public void Address_Constructor_From_IPublicKey_Should_Hash_Public_Key_And_Retain_Last_18_Bytes()
        {
            //https://github.com/catalyst-network/protobuffs-protocol-sdk-csharp/issues/41
        }
    }
}
