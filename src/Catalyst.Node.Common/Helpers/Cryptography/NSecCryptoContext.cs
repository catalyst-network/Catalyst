using System;
using NSec.Cryptography;

namespace Catalyst.Node.Common.Cryptography
{
    /// <summary>
    /// Provides NSec crypto operations on wrapped keys.
    /// </summary>
    public sealed class NSecCryptoContext : ICryptoContext
    {
        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;
        private readonly KeyBlobFormat _publicKeyFormat = KeyBlobFormat.PkixPublicKey;
        private readonly KeyBlobFormat _privateKeyFormat = KeyBlobFormat.PkixPrivateKey;

        public IPrivateKey GeneratePrivateKey(){
            //Newly generated private keys can be exported once.
            var keyParams = new KeyCreationParameters{ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving}; 
            var key = Key.Create(_algorithm, keyParams);
            return new NSecPrivateKeyWrapper(key);
        }

        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            var nSecKey = PublicKey.Import(_algorithm, blob, _publicKeyFormat);
            return new NSecPublicKeyWrapper(nSecKey);
        }
        
        /// <summary>
        /// Exports public key. Can throw unhandled exception or return null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] ExportPublicKey(IPublicKey key)
        {
            return key?.GetNSecFormatPublicKey().Export(_publicKeyFormat);  
        }

        public IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob)
        {
            var nSecKey = Key.Import(_algorithm, blob, _privateKeyFormat);
            return new NSecPrivateKeyWrapper(nSecKey);
        }

        /// <summary>
        /// Exports private key. Can throw unhandled exception or return null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public byte[] ExportPrivateKey(IPrivateKey key)
        {
            return key?.GetNSecFormatPrivateKey().Export(_privateKeyFormat);  


        }

        public byte[] Sign(IPrivateKey privateKey, ReadOnlySpan<byte> data)
        {
            Key realKey = privateKey.GetNSecFormatPrivateKey();
            return _algorithm.Sign(realKey, data);
        }

        public bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            PublicKey realKey = key.GetNSecFormatPublicKey();
            return _algorithm.Verify(realKey, data, signature);
        }

        public IPublicKey GetPublicKey(IPrivateKey key)
        {
            PublicKey realPublicKey = key.GetNSecFormatPublicKey();
            return new NSecPublicKeyWrapper(realPublicKey);
        }
    }
}