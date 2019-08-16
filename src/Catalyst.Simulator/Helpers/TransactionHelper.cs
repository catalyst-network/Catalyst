using System;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace Catalyst.Simulator.Helpers
{
    public static class TransactionHelper
    {
        public static BroadcastRawTransactionRequest GenerateTransaction(int amount, int fee)
        {
            var guid = Guid.NewGuid();
            var broadcastRawTransactionRequest = new BroadcastRawTransactionRequest();
            var transaction = new TransactionBroadcast
            {
                TransactionFees = (ulong) fee,
                TimeStamp = Timestamp.FromDateTime(DateTime.UtcNow),
                LockTime = 0,
                Version = 1
            };

            var stTransactionEntry = new STTransactionEntry
            {
                PubKey = ByteString.FromBase64("VkC84TBQOVjrcX81NYV5swPVrE4RN+nKGzIjxNT2AY0="), Amount = amount
            };

            transaction.STEntries.Add(stTransactionEntry);

            transaction.Signature = new TransactionSignature
            {
                SchnorrSignature = ByteString.CopyFromUtf8($"Signature{guid}"),
                SchnorrComponent = ByteString.CopyFromUtf8($"Component{guid}")
            };

            broadcastRawTransactionRequest.Transaction = transaction;

            return broadcastRawTransactionRequest;
        }
    }
}
