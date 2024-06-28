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
using Catalyst.Core.Modules.Kvm;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.TestUtils
{
    public static class EntryUtils
    {
        public static PublicEntry PrepareContractEntry(Address recipient,
            Address sender,
            UInt256 amount,
            string dataHex = "0x",
            ulong nonce = 0)
        {
            return new PublicEntry
            {
                ReceiverAddress = ByteString.CopyFrom(recipient?.Bytes ?? Bytes.Empty),
                SenderAddress = ByteString.CopyFrom(sender?.Bytes ?? Bytes.Empty),
                Nonce = nonce,
                Amount = amount.ToUint256ByteString(),
                Data = ByteString.CopyFrom(Bytes.FromHexString(dataHex)),
                GasLimit = 21000,
                GasPrice = 0.ToUint256ByteString()
            };
        }

        public static Delta PrepareSingleContractEntryDelta(IPublicKey recipient,
            IPublicKey sender,
            UInt256 amount,
            string dataHex = "0x",
            ulong nonce = 0)
        {
            return new Delta
            {
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                StateRoot = ByteString.CopyFrom(Keccak.EmptyTreeHash.Bytes),
                PublicEntries =
                {
                    PrepareContractEntry(recipient.ToKvmAddress(), sender.ToKvmAddress(), amount, dataHex, nonce)
                }
            };
        }

        public static Delta PrepareSinglePublicEntryDelta(IPublicKey recipient, IPublicKey sender, UInt256 amount)
        {
            return new Delta
            {
                StateRoot = ByteString.CopyFrom(Keccak.EmptyTreeHash.Bytes),
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
                ReceiverAddress = recipient.ToKvmAddressByteString(),
                SenderAddress = sender.ToKvmAddressByteString(),
                Nonce = 0,
                Amount = amount.ToUint256ByteString(),
                GasLimit = 21000
            };
        }
    }
}
