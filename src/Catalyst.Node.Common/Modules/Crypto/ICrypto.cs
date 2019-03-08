using System;
using System.Threading.Tasks;
using Catalyst.Node.Common.Helpers.Cryptography;

namespace Catalyst.Node.Common.Modules.Crypto
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICrypto
    {
        Task<Signature> Sign(ReadOnlySpan<byte> data, string address, string password);
    }
}