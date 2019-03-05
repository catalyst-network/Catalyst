using System;
using Catalyst.Node.Common.Helpers.Cryptography;
using System.Threading.Tasks;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class LocalSignatureProvider : ISignatureProvider
    {
        private readonly ICryptoContext _context;
        
        public LocalSignatureProvider(ICryptoContext context) { this._context = context; }
        
        public Task<Signature> Sign(ReadOnlySpan<byte> data)
        {
            //get key out of keystore but just make one for now.
            IPrivateKey key = _context.GeneratePrivateKey();
            byte[] bytes = _context.Sign(key, data);
            return Task.FromResult(new Signature(bytes));
        }
    }
}