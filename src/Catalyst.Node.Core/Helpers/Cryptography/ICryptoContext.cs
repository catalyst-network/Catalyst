using System;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICryptoContext{

        IKey GenerateKey();

        IPublicKey ImportPublicKey(ReadOnlySpan<byte> blob);
        
        byte[] Sign(IKey key, ReadOnlySpan<byte> data);

        bool Verify(IPublicKey key, ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);
    }
}