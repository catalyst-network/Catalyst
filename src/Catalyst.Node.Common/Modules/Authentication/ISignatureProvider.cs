using System;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Cryptography;

namespace Catalyst.Node.Common.Modules.Authentication
{
    public interface ISignatureProvider
    {
        Task<Signature> Sign(ReadOnlySpan<byte> data, string address, string password);
    }
}