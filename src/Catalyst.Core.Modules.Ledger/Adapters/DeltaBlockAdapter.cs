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
using System.Numerics;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Account;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Ipfs;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.Core.Modules.Ledger.Adapters
{
    public class DeltaBlockAdapter
    {
        public Block DeltaToBlock(Delta delta, MultiHash currentDeltaHash, NetworkType networkType)
        {
            var parentKeccak = new Keccak(delta.PreviousDeltaDfsHash.ToByteArray());
            var ommersKeccak = new Keccak(currentDeltaHash.Digest);
            var timestamp = new UInt256(delta.TimeStamp.Seconds);
            var beneficiary = delta.CoinbaseEntries.First().ReceiverPublicKey.ToByteArray().ToAddress(networkType, AccountType.PublicAccount);
            var beneficiaryAddress = new Nethermind.Core.Address(beneficiary.RawBytes);
            var header = new BlockHeader(parentKeccak, 
                ommersKeccak, 
                beneficiaryAddress, 
                UInt256.Zero, 
                0, 
                long.MaxValue,
                timestamp, 
                new byte[0]);

            var transactions = delta.PublicEntries.Select(e => new Transaction()
            {
                GasPrice = 1,
                Nonce = new UInt256(e.Base.Nonce),
                Data = null,
                DeliveredBy = new PublicKey(e.Base.SenderPublicKey.ToByteArray()),
                GasLimit = (long) e.Base.TransactionFees.ToUInt256(),
                Hash = new Keccak(e.ToByteArray()),
                Init = null,
                SenderAddress = e.Base.SenderPublicKey.ToEthereumAddress(networkType, AccountType.PublicAccount),
                Signature = new Signature(new byte[0]),
                Timestamp = delta.TimeStamp.ToUInt256(),
                To = e.Base.ReceiverPublicKey.ToEthereumAddress(networkType, AccountType.PublicAccount),
                Value = e.Amount.ToUInt256()
            });

            var block = new Block(header, transactions);
            return block;
        }
    }
}
