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

using Catalyst.Protocol.Common;

namespace Catalyst.Protocol.Shared
{
    //https://github.com/catalyst-network/protobuffs-protocol-sdk-csharp/issues/41
    //move that to the Utils project after Dennis' project gets created.

    /// <summary>
    /// An interface to represent the account addresses used by the ledger. 
    /// </summary>
    public interface IAddress
    {
        /// <summary>
        /// Network to which the address belongs.
        /// </summary>
        Network Network { get; }
        
        /// <summary>
        /// Is the address an externally owned address or a smart contract address.
        /// </summary>
        bool IsSmartContract { get;}

        /// <summary>
        /// The full address in bytes
        /// First byte: Network
        /// Second byte: 1 for smart contract address, 0 otherwise
        /// All other bytes (18 bytes): content derived from the public key behind the address
        /// </summary>
        byte[] RawBytes { get; }

        /// <summary>
        /// Base32 crockford representation of the <see cref="RawBytes"/>
        /// </summary>
        string AsBase32Crockford { get; }
    }
}
