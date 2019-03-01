using System;
using Catalyst.Node.Common.Cryptography;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class LocalSignatureProvider : ISignatureProvider
    {
        private readonly ICryptoContext _context;
        
        public LocalSignatureProvider(ICryptoContext context) { this._context = context; }
        
        public byte[] Sign(ReadOnlySpan<byte> data)
        {
            //get key out of keystore but just make one for now.
            IPrivateKey key = _context.GeneratePrivateKey();
            return _context.Sign(key, data);
        }
    }
}