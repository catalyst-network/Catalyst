using System;
using System.Text;
using ADL.Hex.HexConverters.Extensions;
using ADL.Util;

namespace ADL.KeySigner
{
    public class MessageSigner
    {
        public virtual string EcRecover(byte[] hashMessage, string signature)
        {
            var ecdaSignature = ExtractEcdsaSignature(signature);
            return AtlasECKey.RecoverFromSignature(ecdaSignature, hashMessage).GetPublicAddress();
        }

        public byte[] Hash(byte[] plainMessage)
        {
            var hash = new Sha3Keccack().CalculateHash(plainMessage);
            return hash;
        }

        public string HashAndEcRecover(string plainMessage, string signature)
        {
            return EcRecover(Hash(Encoding.UTF8.GetBytes(plainMessage)), signature);
        }

        public string HashAndSign(string plainMessage, string privateKey)
        {
            return HashAndSign(Encoding.UTF8.GetBytes(plainMessage), new AtlasECKey(privateKey.HexToByteArray(), true));
        }

        public string HashAndSign(byte[] plainMessage, string privateKey)
        {
            return HashAndSign(plainMessage, new AtlasECKey(privateKey.HexToByteArray(), true));
        }

        public virtual string HashAndSign(byte[] plainMessage, AtlasECKey key)
        {
            var hash = Hash(plainMessage);
            var signature = key.SignAndCalculateV(hash);
            return CreateStringSignature(signature);
        }

        public string Sign(byte[] message, string privateKey)
        {
            return Sign(message, new AtlasECKey(privateKey.HexToByteArray(), true));
        }

        public virtual string Sign(byte[] message, AtlasECKey key)
        {
            var signature = key.SignAndCalculateV(message);
            return CreateStringSignature(signature);
        }

        public virtual AtlasECDSASignature SignAndCalculateV(byte[] message, string privateKey)
        {
            return new AtlasECKey(privateKey.HexToByteArray(), true).SignAndCalculateV(message);
        }

        private static string CreateStringSignature(AtlasECDSASignature signature)
        {
            return AtlasECDSASignature.CreateStringSignature(signature);
        }

        public static AtlasECDSASignature ExtractEcdsaSignature(string signature)
        {
            return AtlasECDSASignatureFactory.ExtractECDSASignature(signature);
        }
    }
}