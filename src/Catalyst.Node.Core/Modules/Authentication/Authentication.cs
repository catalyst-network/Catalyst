using System;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Helpers.Keystore;
using Catalyst.Node.Common.Modules.Authentication;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.Modules.Authentication
{
    public class Authentication : IAuthentication
    {
        private readonly ICryptoContext _context;
        private readonly ISignatureProvider _signatureProvider;
        
        public Authentication(ICryptoContext context, ISignatureProvider signatureProvider)
        {
            _context = context;
            _signatureProvider = signatureProvider;
        }

        public Task<Signature> Signature(ReadOnlySpan<byte> data, string address, string password) { return _signatureProvider.Sign(data, address, password); }
    }
}