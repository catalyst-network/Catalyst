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

using Catalyst.Abstractions.Cryptography;
using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;
using FluentAssertions;
using Multiformats.Hash.Algorithms;
using NSubstitute;
using Xunit;

namespace Catalyst.Core.Lib.Tests.UnitTests.Utils
{
    public class AddressHelperTests
    {
        private readonly IPeerSettings _peerSettings;
        private readonly IPublicKey _publicKey;
        private readonly IMultihashAlgorithm _hashAlgorithm;

        public AddressHelperTests()
        {
            _peerSettings = Substitute.For<IPeerSettings>();
            _publicKey = Substitute.For<IPublicKey>();
            _hashAlgorithm = AddressHelper.HashAlgorithm;
        }

        [Theory]
        [InlineData(NetworkType.Devnet, AccountType.ConfidentialAccount)]
        [InlineData(NetworkType.Mainnet, AccountType.PublicAccount)]
        [InlineData(NetworkType.Testnet, AccountType.SmartContractAccount)]
        public void AddressHelper_should_use_PeerSettings_NetworkType(NetworkType networkType, AccountType accountType)
        {
            _peerSettings.NetworkType.Returns(networkType);

            var pubKeyBytes = ByteUtil.GenerateRandomByteArray(Ffi.PublicKeyLength);
            var expectedHash = pubKeyBytes.ComputeMultihash(_hashAlgorithm).Digest;
            
            var addressHelper = new AddressHelper(_peerSettings);

            var address = addressHelper.GenerateAddress(_publicKey, accountType);
            _ = _publicKey.Received(1).Bytes;

            address.AccountType.Should().Be(accountType);
            address.NetworkType.Should().Be(networkType);
            address.PublicKeyHash.ToByteArray().Should().BeEquivalentTo(expectedHash);
        }
    }
}

