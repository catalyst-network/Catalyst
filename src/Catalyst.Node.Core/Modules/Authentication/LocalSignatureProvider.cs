using System;
using Catalyst.Node.Common.Helpers.Cryptography;
using Catalyst.Node.Common.Interfaces;
using System.Threading.Tasks;

namespace Catalyst.Node.Core.Modules.Authentication
{
    public class LocalSignatureProvider : ISignatureProvider
    {
        private readonly ICryptoContext _context;
        private readonly IKeystore _keystore;

        public LocalSignatureProvider(ICryptoContext context, IKeystore keystore)
        {
            this._context = context;
            this._keystore = keystore;
        }
        
        public Task<Signature> Sign(ReadOnlySpan<byte> data, string address, string password)
        {
            IPrivateKey key = _keystore.RetrieveKey(address, password);
            byte[] bytes = _context.Sign(key, data);
            return Task.FromResult(new Signature(bytes));
        }
    }
}