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
using Catalyst.Protocol.Transaction;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.TestUtils
{
    public static class TransactionHelper
    {
        public static TransactionBroadcast GetTransaction(uint standardAmount = 123,
            string standardPubKey = "standardPubKey",
            string signature = "signature",
            string confidentialCommitment = "confidentialCommitment",
            string confidentialPubKey = "confidentialPubKey",
            TransactionType transactionType = TransactionType.Normal,
            long timeStamp = 12345,
            ulong transactionFees = 2,
            ulong lockTime = 9876)
        {
            var transaction = new TransactionBroadcast
            {
                STEntries =
                {
                    new STTransactionEntry
                    {
                        Amount = standardAmount,
                        PubKey = standardPubKey.ToUtf8ByteString()
                    }
                },
                CFEntries =
                {
                    new CFTransactionEntry
                    {
                        PedersenCommit = confidentialCommitment.ToUtf8ByteString(),
                        PubKey = confidentialPubKey.ToUtf8ByteString()
                    }
                },
                Signature = signature.ToUtf8ByteString(),
                TransactionType = transactionType,

                TimeStamp = new Timestamp
                {
                    Seconds = timeStamp
                },
              
                TransactionFees = transactionFees,
                LockTime = lockTime
            };
            return transaction;
        }
    }
}
