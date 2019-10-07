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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.P2P;
using Catalyst.Abstractions.Util;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Network;

namespace Catalyst.Core.Lib.Util
{
    //public sealed class AddressHelper : IAddressHelper
    //{
    //    private readonly IHashProvider _hashAlgorithm;

    //    public AddressHelper(IHashProvider hashAlgorithm) { _hashAlgorithm = hashAlgorithm; }

    //    private readonly NetworkType _networkType;

    //    public AddressHelper(IPeerSettings peerSettings) : this(peerSettings.NetworkType) { }

    //    public AddressHelper(NetworkType networkType) { _networkType = networkType; }

    //    /// <inheritdoc />
    //    /// @todo
    //    public Address GenerateAddress(IPublicKey publicKey, AccountType accountType)
    //    {
    //        var publicKeyHash = _hashAlgorithm.ComputeRawHash(publicKey.Bytes).ToByteString();
    //        var address = new Address
    //        {
    //            PublicKeyHash = publicKeyHash,
    //            AccountType = accountType,
    //            NetworkType = _networkType
    //        };
    //        return address;
    //    }
    //}
}
