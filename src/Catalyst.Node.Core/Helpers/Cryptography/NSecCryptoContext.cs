using System;
using NSec.Cryptography;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public class NSecCryptoContext : ICryptoContext{

        private static SignatureAlgorithm algorithm = SignatureAlgorithm.Ed25519;

        public IKey GenerateKey(){
            Key key = Key.Create(algorithm);
            return new NSecKeyWrapper(key);
        }


        public byte[] Sign (IKey key, ReadOnlySpan<byte> data)
        {
            Key realKey = key.GetNSecFormatKey();
            return algorithm.Sign(realKey, new ReadOnlySpan<byte>());
        }

        public bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
        {
            PublicKey realKey = key.GetNSecFormatPublicKey();
            return algorithm.Verify(realKey, data, signature);

        }
        
        
    }
}