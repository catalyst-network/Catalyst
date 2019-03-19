using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
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
                STEntries = { new STTransactionEntry {
                    Amount = standardAmount,
                    PubKey = standardPubKey.ToUtf8ByteString()
                }},
                CFEntries = { new CFTransactionEntry()
                {
                    CommPedersenCommitmentitment = confidentialCommitment.ToUtf8ByteString(),
                    PubKey = confidentialPubKey.ToUtf8ByteString()
                }},
                Signature = GetTransactionSignature(challenge, signature),
                Version = version,
                TimeStamp = timeStamp,
                TransactionFees = transactionFees,
                LockTime = lockTime 
            };
            return transaction;
        }

        public static TransactionSignature GetTransactionSignature(
            string signature = "signature",
            string challenge = "challenge")
        {
            return new TransactionSignature
            {
                Challenge = challenge.ToUtf8ByteString(),
                Signature = signature.ToUtf8ByteString()
            };
        }
    }
}
