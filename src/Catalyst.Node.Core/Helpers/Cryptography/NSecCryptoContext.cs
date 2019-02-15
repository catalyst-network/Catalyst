using System;
using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    /// <summary>
    /// Provides NSec crypto operations on wrapped keys.
    /// </summary>
    public sealed class NSecCryptoContext : ICryptoContext{

        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;

        public IPrivateKey GeneratePrivateKey(){
            Key key = Key.Create(_algorithm);
            return new NSecPrivateKeyWrapper(key);
        }

        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            bool imported = PublicKey.TryImport(_algorithm, blob,
                KeyBlobFormat.PkixPublicKey, out var nsecKey);
            return imported ? new NSecPublicKeyWrapper(nsecKey) : null;
        }
        
        public byte[] ExportPublicKey(IPublicKey key)
        {
            return key?.GetNSecFormatPublicKey().Export(KeyBlobFormat.PkixPublicKey);  
        }

        public IPrivateKey ImportPrivateKey(ReadOnlySpan<byte> blob)
        {
            bool imported = Key.TryImport(_algorithm, blob,
                KeyBlobFormat.PkixPrivateKey, out var nsecKey);
            return imported ? new NSecPrivateKeyWrapper(nsecKey) : null;
        }

        public byte[] ExportPrivateKey(IPrivateKey key)
        {
            return key?.GetNSecFormatPrivateKey().Export(KeyBlobFormat.PkixPrivateKey); 
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