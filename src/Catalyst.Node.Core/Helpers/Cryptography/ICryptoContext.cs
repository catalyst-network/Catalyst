using System;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICryptoContext{

        IKey GenerateKey();

        //IPublicKey ImportPublicKey(ReadOnly);
        
        byte[] Sign(IKey key, ReadOnlySpan<byte> data);

        bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);
    }
}