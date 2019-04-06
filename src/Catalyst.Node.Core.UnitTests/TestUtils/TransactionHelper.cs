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

using Catalyst.Protocol.Transaction;
using Catalyst.Node.Common.Helpers;

namespace Catalyst.Node.Core.UnitTest.TestUtils
{
    public static class TransactionHelper
    {
        public static Transaction GetTransaction(uint standardAmount = 123,
            string standardPubKey = "standardPubKey",
            string signature = "signature",
            string challenge = "challenge",
            string confidentialCommitment = "confidentialCommitment",
            string confidentialPubKey = "confidentialPubKey",
            uint version = 1,
            ulong timeStamp = 12345,
            ulong transactionFees = 2,
            ulong lockTime = 9876)
        {
            var transaction = new Transaction()
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
                Signature = GetTransactionSignature(signature, challenge),
                Version = version,
                TimeStamp = timeStamp,
                TransactionFees = transactionFees,
                LockTime = lockTime
            };
            return transaction;
        }

        public static TransactionSignature GetTransactionSignature(string signature = "signature",
            string challenge = "challenge")
        {
            return new TransactionSignature
            {
                SchnorrComponent = challenge.ToUtf8ByteString(),
                SchnorrSignature = signature.ToUtf8ByteString()
            };
        }
    }
}
