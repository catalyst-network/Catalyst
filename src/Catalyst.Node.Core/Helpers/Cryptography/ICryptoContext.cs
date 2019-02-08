using System;

namespace Catalyst.Node.Core.Helpers.Cryptography
{
    public interface ICryptoContext{

        IKey GenerateKey();
        byte[] Sign(IKey key, ReadOnlySpan<byte> data);

        //bool Verify(IKey key, );
    }
}