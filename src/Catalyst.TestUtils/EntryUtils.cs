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
using Catalyst.Abstractions.Cryptography;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.TestUtils
{
    public static class EntryUtils
    {
        public static PublicEntry PrepareContractEntry(IPublicKey recipient, IPublicKey sender, UInt256 amount,  string dataHex = "0x", ulong nonce = 0)
        {
            return new PublicEntry
            {
                Base = new BaseEntry
                {
                    ReceiverPublicKey = recipient == null ? ByteString.Empty : ByteString.CopyFrom(recipient.Bytes),
                    SenderPublicKey = ByteString.CopyFrom(sender.Bytes),
                    TransactionFees = ByteString.CopyFrom(1),
                    Nonce = nonce
                },
                Amount = amount.ToUint256ByteString(),
                Data = ByteString.CopyFrom(Bytes.FromHexString(dataHex)),
                GasLimit = 21000,
                GasPrice = 0,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
        
        public static Delta PrepareSingleContractEntryDelta(IPublicKey recipient, IPublicKey sender, UInt256 amount, string dataHex = "0x", ulong nonce = 0)
        {
            return new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    PrepareContractEntry(recipient, sender, amount, dataHex, nonce)
                }
            };
        }
        
        public static Delta PrepareSinglePublicEntryDelta(IPublicKey recipient, IPublicKey sender, UInt256 amount)
        {
            return new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                PublicEntries =
                {
                    PreparePublicEntry(recipient, sender, amount)
                }
            };
        }

        public static PublicEntry PreparePublicEntry(IPublicKey recipient, IPublicKey sender, UInt256 amount)
        {
            return new PublicEntry
            {
                Base = new BaseEntry
                {
                    ReceiverPublicKey = ByteString.CopyFrom(recipient.Bytes),
                    SenderPublicKey = ByteString.CopyFrom(sender.Bytes),
                    TransactionFees = ByteString.CopyFrom(1),
                    Nonce = 0
                },
                Amount = amount.ToUint256ByteString(),
                GasLimit = 21000,
                Timestamp = Timestamp.FromDateTime(DateTime.UtcNow)
            };
        }
    }
}
