using System.IO;
using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class NethereumKeystore : NethereumKeystoreBase, IKeystore
    {
        private readonly ICryptoContext _context;

        public NethereumKeystore(ICryptoContext context) { _context = context; }

        public void StoreKey(IPrivateKey key, string password)
        {
            byte[] rawKey = _context.ExportPrivateKey(key);
            string address = _context.AddressFromKey(key);
            StoreKey(rawKey, address, password);
        }
        
        public IPrivateKey RetrieveKey(string address, string password)
        {
            
            var key = RetrieveKeyBytes(address,password);
            return _context.ImportPrivateKey(key);

        }

        
        
    }
}