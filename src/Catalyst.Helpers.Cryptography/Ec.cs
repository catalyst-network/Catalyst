using System;
using System.Text;
using Catalyst.Helpers.Logger;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Catalyst.Helpers.Cryptography
{
    public class Ec
    {
        private static readonly SecureRandom SecureRandom = new SecureRandom();

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static AsymmetricCipherKeyPair CreateKeyPair()
        {
            var gen = new ECKeyPairGenerator("EC");
            var keyGenerationParams = new KeyGenerationParameters(SecureRandom, 256);
            gen.Init(keyGenerationParams);
            var keyPair = gen.GenerateKeyPair();
            var privateBytes = ((ECPrivateKeyParameters) keyPair.Private).D.ToByteArray();
            if (privateBytes.Length != 32)
                return CreateKeyPair();
            return keyPair;
        }

        /// <summary>
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="privKey"></param>
        /// <returns></returns>
        public static string SignData(string msg, AsymmetricKeyParameter privKey)
        {
            try
            {
                var msgBytes = Encoding.UTF8.GetBytes(msg);

                var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(true, privKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                var sigBytes = signer.GenerateSignature();

                return Convert.ToBase64String(sigBytes);
            }
            catch (Exception exc)
            {
                Log.Message("Signing Failed: " + exc);
                return null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="pubKey"></param>
        /// <param name="signature"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static bool VerifySignature(AsymmetricKeyParameter pubKey, string signature, string msg)
        {
            try
            {
                var msgBytes = Encoding.UTF8.GetBytes(msg);
                var sigBytes = Convert.FromBase64String(signature);

                var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
                signer.Init(false, pubKey);
                signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
                return signer.VerifySignature(sigBytes);
            }
            catch (Exception exc)
            {
                Log.Message("Verification failed with the error: " + exc);
                return false;
            }
        }
    }
}