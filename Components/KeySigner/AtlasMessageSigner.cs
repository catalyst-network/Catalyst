using System.Collections.Generic;
using System.Text;
using ADL.Hex.HexConvertors.Extensions;

namespace ADL.KeySigner
{
    public class AtlasereumMessageSigner : MessageSigner
    {
        public override string EcRecover(byte[] message, string signature)
        {
            return base.EcRecover(HashPrefixedMessage(message), signature);
        }

        public byte[] HashAndHashPrefixedMessage(byte[] message)
        {
            return HashPrefixedMessage(Hash(message));
        }

        public override string HashAndSign(byte[] plainMessage, AtlasECKey key)
        {
            return base.Sign(HashAndHashPrefixedMessage(plainMessage), key);
        }

        public byte[] HashPrefixedMessage(byte[] message)
        {
            var byteList = new List<byte>();
            var bytePrefix = "0x19".HexToByteArray();
            var textBytePrefix = Encoding.UTF8.GetBytes("Atlasereum Signed Message:\n" + message.Length);

            byteList.AddRange(bytePrefix);
            byteList.AddRange(textBytePrefix);
            byteList.AddRange(message);
            return Hash(byteList.ToArray());
        }

        public override string Sign(byte[] message, AtlasECKey key)
        {
            return base.Sign(HashPrefixedMessage(message), key);
        }

        public string EncodeUTF8AndSign(string message, AtlasECKey key)
        {
            return base.Sign(HashPrefixedMessage(Encoding.UTF8.GetBytes(message)), key);
        }

        public string EncodeUTF8AndEcRecover(string message, string signature)
        {
            return EcRecover(Encoding.UTF8.GetBytes(message), signature);
        }
    }
}