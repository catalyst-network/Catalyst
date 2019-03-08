using System;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Common.Modules.Crypto;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.Modules.Crypto
{
    public class Crypto : ICrypto
    {
        private readonly ICryptoContext _context;
        private readonly ISignatureProvider _signatureProvider;
        
        public Crypto(ICryptoContext context, ISignatureProvider signatureProvider)
        {
            _context = context;
            _signatureProvider = signatureProvider;
        }
        public Task<Signature> Sign(ReadOnlySpan<byte> data, string address, string password) { return _signatureProvider.Sign(data, address, password); }
    }
}