using System;
using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    /// <summary>
    /// Provides NSec crypto operations on wrapped keys.
    /// </summary>
    public sealed class NSecCryptoContext : ICryptoContext{

        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;

        public IKey GenerateKey(){
            Key key = Key.Create(_algorithm);
            return new NSecKeyWrapper(key);
        }

        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            bool imported = PublicKey.TryImport(_algorithm, blob,
                KeyBlobFormat.PkixPublicKey, out PublicKey nsecKey);
            return imported ? new NSecPublicKeyWrapper(nsecKey) : null;
        }
        
        public byte[] ExportPublicKey(IPublicKey key)
        {
            return key?.GetNSecFormatPublicKey().Export(KeyBlobFormat.PkixPublicKey);  
        }

        public byte[] Sign (IKey key, ReadOnlySpan<byte> data)
        {
            Key realKey = key.GetNSecFormatKey();
            return _algorithm.Sign(realKey, data);
        }

        public bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            PublicKey realKey = key.GetNSecFormatPublicKey();
            return _algorithm.Verify(realKey, data, signature);
        }
        
        
    }
}