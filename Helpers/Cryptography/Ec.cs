using System;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace ADL.Cryptography
{
    public class Ec
    {
        private static readonly SecureRandom SecureRandom = new SecureRandom();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static AsymmetricCipherKeyPair CreateKeyPair()
        {
            ECKeyPairGenerator gen = new ECKeyPairGenerator("EC");
            KeyGenerationParameters keyGenerationParams = new KeyGenerationParameters(SecureRandom, 256);
            gen.Init(keyGenerationParams);
            AsymmetricCipherKeyPair keyPair = gen.GenerateKeyPair();
            var privateBytes = ((ECPrivateKeyParameters) keyPair.Private).D.ToByteArray();
            if (privateBytes.Length != 32)
                return CreateKeyPair();
            return keyPair;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="privKey"></param>
        /// <returns></returns>
        public static string SignData(string msg, AsymmetricKeyParameter privKey)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);

                ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(true, privKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                byte[] sigBytes = signer.GenerateSignature();

                return Convert.ToBase64String(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Signing Failed: " + exc.ToString());
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pubKey"></param>
        /// <param name="signature"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool VerifySignature(AsymmetricKeyParameter pubKey, string signature, string msg)
        {
            try
            {
                byte[] msgBytes = Encoding.UTF8.GetBytes(msg);
                byte[] sigBytes = Convert.FromBase64String(signature);

                ISigner signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(false, pubKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (Exception exc)
            {
                Console.WriteLine("Verification failed with the error: " + exc.ToString());
                return false;
            }
        }
    }
}
