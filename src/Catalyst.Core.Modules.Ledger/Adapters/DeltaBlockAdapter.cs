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
using Catalyst.Core.Lib.Extensions.Protocol.Account;
using Catalyst.Protocol.Account;
using Catalyst.Protocol.Deltas;
using Catalyst.Protocol.Network;
using Ipfs;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using NLog.LayoutRenderers.Wrappers;

namespace Catalyst.Core.Modules.Ledger.Adapters
{
    public class DeltaBlockAdapter
    {
        public Block DeltaToBlock(Delta delta, MultiHash currentDeltaHash, NetworkType networkType)
        {
            var parentKeccak = new Keccak(delta.PreviousDeltaDfsHash.ToByteArray());
            var ommersKeccak = new Keccak(currentDeltaHash.Digest);
            var timestamp = new UInt256(new BigInteger(delta.TimeStamp.Seconds) * new BigInteger(Math.Pow(10, 9)) + delta.TimeStamp.Nanos);
            var beneficiary = delta.CoinbaseEntries.First().ReceiverPublicKey.ToByteArray().ToAddress(networkType, AccountType.PublicAccount);
            var beneficiaryAddress = new Nethermind.Core.Address(beneficiary.RawBytes);
            var header = new BlockHeader(parentKeccak, ommersKeccak, beneficiaryAddress, UInt256.Zero, 0, long.MaxValue, timestamp, new []{});
            
            var block = new Block(header);
            return block;
        }
    }
}
