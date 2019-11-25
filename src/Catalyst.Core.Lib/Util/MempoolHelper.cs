using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.Mempool.Models;
using Catalyst.Protocol.Wire;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Lib.Util
{
    public static class MempoolHelper
    {
        public static IEnumerable<MempoolItem> GetMempoolItems(TransactionBroadcast transactionBroadcast, IHashProvider hashProvider)
        {
            foreach (var publicEntry in transactionBroadcast.PublicEntries)
            {
                var mempoolItem = new MempoolItem();
                mempoolItem.Id = MultiBase.Encode(hashProvider.ComputeMultiHash(transactionBroadcast.Signature.ToByteArray()).ToArray(), "base32");
                mempoolItem.Amount = publicEntry.Amount.ToUInt256().ToString();
                mempoolItem.ReceiverAddress = publicEntry.Base.ReceiverPublicKey.ToByteArray().ToBase32();
                mempoolItem.SenderAddress = publicEntry.Base.SenderPublicKey.ToByteArray().ToBase32();
                mempoolItem.Timestamp = transactionBroadcast.Timestamp.ToDateTime();
                mempoolItem.Signature = transactionBroadcast.Signature.ToByteArray().ToBase32();
                mempoolItem.Nonce = publicEntry.Base.Nonce;
                yield return mempoolItem;
            }
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
                    writer.Write(mempoolItem.Signature.FromBase32());
                    writer.Write(BitConverter.GetBytes(mempoolItem.Timestamp.Ticks));
                    writer.Write(mempoolItem.Amount);
                    writer.Write(mempoolItem.Nonce);
                    writer.Write(mempoolItem.ReceiverAddress);
                    writer.Write(mempoolItem.SenderAddress);
                    writer.Write(mempoolItem.Fee);
                    writer.Write(mempoolItem.Data);
                }
                mempoolItem.Id = hashProvider.ComputeMultiHash(memoryStream).ToBase32();
            }
        }
    }
}
