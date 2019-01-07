﻿using ADL.Hex.HexConvertors.Extensions;

namespace ADL.KeySigner
{
    public class TransactionFactory
    {
        public static TransactionBase CreateTransaction(string rlpHex)
        {
            return CreateTransaction(rlpHex.HexToByteArray());
        }

        public static TransactionBase CreateTransaction(byte[] rlp)
        {
            var rlpSigner = TransactionBase.CreateDefaultRLPSigner(rlp);
            return rlpSigner.IsVSignatureForChain()
                ? (TransactionBase) new TransactionChainId(rlpSigner)
                : new Transaction(rlpSigner);
        }
    }
}