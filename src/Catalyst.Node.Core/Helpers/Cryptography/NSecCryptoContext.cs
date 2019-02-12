using System;
using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public sealed class NSecCryptoContext : ICryptoContext{

        private readonly SignatureAlgorithm _algorithm = SignatureAlgorithm.Ed25519;

        public IKey GenerateKey(){
            Key key = Key.Create(_algorithm);
            return new NSecKeyWrapper(key);
        }

        public IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob)
        {
            var nsecKey = new PublicKey(_algorithm);
            bool imported = PublicKey.TryImport(_algorithm, blob, KeyBlobFormat.PkixPublicKey, out nsecKey);
            return imported ? new NSecPublicKeyWrapper(nsecKey) : new NSecPublicKeyWrapper();
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