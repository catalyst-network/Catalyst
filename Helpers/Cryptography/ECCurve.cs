namespace ADL.Cryptography
{
    public class ECCurve : ICryptography
    {
        public static readonly System.Security.Cryptography.ECCurve Secp256k1 =
            System.Security.Cryptography.ECCurve.CreateFromValue("1.3.132.0.10");

        public static readonly System.Security.Cryptography.ECCurve Secp256r1 =
            System.Security.Cryptography.ECCurve.CreateFromValue("1.2.840.10045.3.1.7");
        //  System.Security.Cryptography.ECCurve.CreateFromFriendlyName("ECDSA_P256_OID_VALUE");
    }
}
