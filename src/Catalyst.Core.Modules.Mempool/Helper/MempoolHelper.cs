using Catalyst.Abstractions.Hashing;
using Catalyst.Core.Lib.Mempool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using TheDotNetLeague.MultiFormats.MultiBase;

namespace Catalyst.Core.Modules.Mempool.Helper
{
    public static class MempoolHelper
    {
        public static void GenerateMempoolItemId(IEnumerable<MempoolItem> mempoolItems, IHashProvider hashProvider)
        {
            foreach(var mempoolItem in mempoolItems)
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
