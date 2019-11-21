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

using Catalyst.Core.Lib.Extensions;
using Catalyst.Protocol.Cryptography;
using Catalyst.Protocol.Network;
using Catalyst.Protocol.Transaction;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethermind.Dirichlet.Numerics;

namespace Catalyst.TestUtils
{
    public static class TransactionHelper
    {
        public static TransactionBroadcast GetPublicTransaction(uint amount = 123,
            string senderPublicKey = "sender",
            string receiverPublicKey = "receiver",
            string signature = "signature",
            long timestamp = 12345,
            ulong transactionFees = 2,
            ulong nonce = 0,
            NetworkType networkType = NetworkType.Devnet)
        {
            var transaction = new TransactionBroadcast
            {
                PublicEntries =
                {
                    new PublicEntry
                    {
                        Amount = ((UInt256) amount).ToUint256ByteString(),
                        Base = new BaseEntry
                        {
                            Nonce = nonce,
                            ReceiverPublicKey = receiverPublicKey.ToUtf8ByteString(),
                            SenderPublicKey = senderPublicKey.ToUtf8ByteString(),
                            TransactionFees = ((UInt256) transactionFees).ToUint256ByteString(),
                        }
                    }
                },
                Timestamp = new Timestamp {Seconds = timestamp},
                Signature = new Signature
                {
                    SigningContext = new SigningContext {NetworkType = networkType, SignatureType = SignatureType.TransactionPublic},
                    RawBytes = signature.ToUtf8ByteString()
                }
            };
            return transaction;
        }
        
        public static TransactionBroadcast GetContractTransaction(ByteString data,
            UInt256 amount,
            uint gasLimit,
            UInt256 gasPrice,
            byte[] targetContract = null, // to be reviewed
            string senderPublicKey = "sender",
            string receiverPublicKey = "receiver",
            string signature = "signature",
            long timestamp = 12345,
            ulong transactionFees = 2,
            ulong nonce = 0,
            NetworkType networkType = NetworkType.Devnet)
        {
            var transaction = new TransactionBroadcast
            {
                PublicEntries =
                {
                    new PublicEntry
                    {
                        Amount = amount.ToUint256ByteString(),
                        Base = new BaseEntry
                        {
                            Nonce = nonce,
                            ReceiverPublicKey = receiverPublicKey.ToUtf8ByteString(),
                            SenderPublicKey = senderPublicKey.ToUtf8ByteString(),
                            TransactionFees = ((UInt256) transactionFees).ToUint256ByteString(),
                        },
                        Data = data,
                        GasLimit = gasLimit,
                        GasPrice = gasPrice,
                        TargetContract = targetContract
                    }
                },
                Timestamp = new Timestamp {Seconds = timestamp},
                Signature = new Signature
                {
                    SigningContext = new SigningContext {NetworkType = networkType, SignatureType = SignatureType.TransactionPublic},
                    RawBytes = signature.ToUtf8ByteString()
                }
            };

            return transaction;
        }
    }
}
