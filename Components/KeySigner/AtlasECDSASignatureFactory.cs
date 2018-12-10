namespace ADL.KeySigner
{
    public class AtlasECDSASignatureFactory
    {
        public static AtlasECDSASignature FromComponents(byte[] r, byte[] s)
        {
            return new AtlasECDSASignature(ECDSASignatureFactory.FromComponents(r, s));
        }

        public static AtlasECDSASignature FromComponents(byte[] r, byte[] s, byte v)
        {
            var signature = FromComponents(r, s);
            signature.V = new[] {v};
            return signature;
        }

        public static AtlasECDSASignature FromComponents(byte[] r, byte[] s, byte[] v)
        {
            return new AtlasECDSASignature(ECDSASignatureFactory.FromComponents(r, s, v));
        }

        public static AtlasECDSASignature FromComponents(byte[] rs)
        {
            return new AtlasECDSASignature(ECDSASignatureFactory.FromComponents(rs));
        }

        public static AtlasECDSASignature ExtractECDSASignature(string signature)
        {
            return new AtlasECDSASignature(ECDSASignatureFactory.ExtractECDSASignature(signature));
        }
    }
}