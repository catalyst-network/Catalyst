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
using Catalyst.Core.Modules.Cryptography.BulletProofs;
using Catalyst.Protocol.Wire;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Simulator.Helpers
{
    public static class TransactionHelper
    {
        public static BroadcastRawTransactionRequest GenerateTransaction(uint amount, int fee)
        {
            var cryptoWrapper = new CryptoWrapper();
            var privateKey = cryptoWrapper.GeneratePrivateKey();
            var publicKey = ByteString.CopyFrom(privateKey.GetPublicKey().Bytes);

            var broadcastRawTransactionRequest = new BroadcastRawTransactionRequest();
            var transaction = new TransactionBroadcast
            {
                TransactionFees = (ulong) fee,
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                LockTime = 0,
                TransactionType = TransactionType.Normal,
                Data = ByteString.CopyFromUtf8("tw:Hello world"),
                Init = ByteString.Empty,
                From = publicKey
            };

            var stTransactionEntry = new STTransactionEntry
            {
                PubKey = publicKey,
                Amount = amount
            };

            transaction.STEntries.Add(stTransactionEntry);

            transaction.Signature = GenerateSignature(cryptoWrapper, privateKey, transaction);

            broadcastRawTransactionRequest.Transaction = transaction;

            return broadcastRawTransactionRequest;
        }

        private static ByteString GenerateSignature(IWrapper cryptoWrapper, IPrivateKey privateKey, TransactionBroadcast transactionBroadcast)
        {
            var transactionWithoutSig = transactionBroadcast.Clone();
            transactionWithoutSig.Signature = ByteString.Empty;

            byte[] signatureBytes = cryptoWrapper.StdSign(privateKey, transactionWithoutSig.ToByteArray(),
                new SigningContext
                {
                    NetworkType = Network.Devnet,
                    SignatureType = SignatureType.TransactionPublic
                }.ToByteArray()).SignatureBytes;
            return ByteString.CopyFrom(signatureBytes);
        }
    }
}
