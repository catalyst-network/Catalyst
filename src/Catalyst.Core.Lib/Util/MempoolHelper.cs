using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.Mempool.Models;
using Catalyst.Protocol.Transaction;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Catalyst.Core.Lib.Util
{
    public static class MempoolHelper
    {
        //public static IEnumerable<MempoolItem> GetMempoolItems(TransactionBroadcast transactionBroadcast, IHashProvider hashProvider)
        //{
        //    var mempoolItem = new MempoolItem();
        //    mempoolItem.Id = MultiBase.Encode(hashProvider.ComputeMultiHash(transactionBroadcast.PublicEntry.Signature.ToByteArray()).ToArray(), "base32");
        //    mempoolItem.Amount = transactionBroadcast.PublicEntry.Amount.ToUInt256().ToString();
        //    mempoolItem.ReceiverAddress = transactionBroadcast.PublicEntry.ReceiverPublicKey.ToByteArray();
        //    mempoolItem.SenderAddress = transactionBroadcast.PublicEntry.SenderPublicKey.ToByteArray();
        //    mempoolItem.Timestamp = transactionBroadcast.PublicEntry.Timestamp.ToDateTime();
        //    mempoolItem.Signature = transactionBroadcast.PublicEntry.Signature.ToByteArray();
        //    mempoolItem.Nonce = transactionBroadcast.PublicEntry.Nonce;
        //    yield return mempoolItem;
        //}

        public static string GetId(PublicEntry publicEntry, IHashProvider hashProvider)
        {
            return hashProvider.ComputeMultiHash(publicEntry.ToByteArray()).ToBase32();
        }

        public static void GenerateMempoolItemId(IEnumerable<MempoolItem> mempoolItems, IHashProvider hashProvider)
        {
            foreach (var mempoolItem in mempoolItems)
            {
                GenerateMempoolItemId(mempoolItem, hashProvider);
            }
        }
        public static void GenerateMempoolItemId(MempoolItem mempoolItem, IHashProvider hashProvider)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    //writer.Write(mempoolItem.Signature);
                    writer.Write(BitConverter.GetBytes(mempoolItem.Timestamp.Ticks));
                    writer.Write(mempoolItem.SenderAddress);
                    writer.Write(mempoolItem.ReceiverAddress);
                    writer.Write(mempoolItem.Amount);
                    writer.Write(mempoolItem.Fee);
                    writer.Write(mempoolItem.Data);
                    writer.Write(mempoolItem.Nonce);
                }
                mempoolItem.Id = hashProvider.ComputeMultiHash(memoryStream).ToBase32();
            }
        }
    }
}
