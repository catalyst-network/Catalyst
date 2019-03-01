using System;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public interface ISignatureProvider
    {
        Byte[] Sign(ReadOnlySpan<Byte> data);
    }
}