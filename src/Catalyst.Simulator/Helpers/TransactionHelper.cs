#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Extensions.Protocol.Wire;
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Int256;

namespace Catalyst.Simulator.Helpers
{
    public static class TransactionHelper
    {
        private static readonly SigningContext DevNetPublicTransactionContext = new SigningContext
        {
            NetworkType = NetworkType.Devnet,
            SignatureType = SignatureType.TransactionPublic
        };

        public static BroadcastRawTransactionRequest GenerateTransaction(uint amount, int nonce = 0)
        {
            var cryptoWrapper = new FfiWrapper();
            var privateKey = cryptoWrapper.GeneratePrivateKey();
            var publicKey = ByteString.CopyFrom(privateKey.GetPublicKey().Bytes);

            var transaction = new TransactionBroadcast
            {
                PublicEntry = new PublicEntry
                {
                    Amount = ((UInt256) amount).ToUint256ByteString(),
                    Nonce = (ulong) nonce,
                    SenderAddress = privateKey.GetPublicKey().Bytes.ToByteString(),
                    ReceiverAddress = publicKey
                }.Sign(cryptoWrapper, privateKey, DevNetPublicTransactionContext)
            };

            var broadcastRawTransactionRequest = new BroadcastRawTransactionRequest
            {
                Transaction = transaction
            };

            return broadcastRawTransactionRequest;
        }
    }
}
