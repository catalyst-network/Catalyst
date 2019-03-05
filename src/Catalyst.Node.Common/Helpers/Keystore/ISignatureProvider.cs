using System;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Cryptography;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public interface ISignatureProvider
    {
        Task<Signature> Sign(ReadOnlySpan<byte> data);
    }
}