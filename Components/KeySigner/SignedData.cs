namespace ADL.KeySigner
{
    public class SignedData
    {
        public byte[][] Data { get; set; }
        public byte[] V { get; set; }
        public byte[] R { get; set; }
        public byte[] S { get; set; }

        public AtlasECDSASignature GetSignature()
        {
            if (!IsSigned()) return null;
            return AtlasECDSASignatureFactory.FromComponents(R, S, V);
        }

        public bool IsSigned()
        {
            return (V != null);
        }

        public SignedData()
        {

        }

        public SignedData(byte[][] data, AtlasECDSASignature signature)
        {
            Data = data;
            if (signature != null)
            {
                R = signature.R;
                S = signature.S;
                V = signature.V;
            }
        }
    }
}