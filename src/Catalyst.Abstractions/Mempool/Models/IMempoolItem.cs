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
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Abstractions.Mempool.Models
{
    public interface IMempoolItem
    {
        ISignature Signature { get; set; }
        Timestamp Timestamp { get; set; } //  records the transaction creation time
        byte[] Amount { get; set; } // uint256 amount
        long Nonce { get; set; } // A nonce, similar to Ethereum, incremented on each transaction on the account issuing the transaction
        byte[] ReciverAddress { get; set; } // PublicKey of receiver.
        byte[] SenderAddress { get; set; } // PublicKey of sender.
        byte[] Fee { get; set; } // 8 bytes, clear text, fees * 10^12
        byte[] Data { get; set; } // Smart contract data.
    }
}
