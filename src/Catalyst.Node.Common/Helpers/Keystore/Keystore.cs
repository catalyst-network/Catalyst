using System.IO;
using Catalyst.Node.Common.Interfaces;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.KeyStore;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class Keystore
    {
        private ICryptoContext _context;

        public void StoreKey(IPrivateKey key, string password)
        {
            byte[] rawKey = _context.ExportPrivateKey(key);
            string address = _context.AddressFromKey(key);
            StoreKey(rawKey, address, password);
        }
        
        public void StoreKey(byte[] privateKey, string address, string password)
        {
            
            
            var service = new KeyStoreService();
            
            var json = service.EncryptAndGenerateDefaultKeyStoreAsJson(password, privateKey, address);

            var fileName = service.GenerateUTCFileName(address);

            using (var newfile = File.CreateText(fileName))
            {            
                newfile.Write(json);
                newfile.Flush();
            }
        }
        
        public IPrivateKey RetrieveKey(string address, string password)
        {
            
            var key = RetrieveKeyBytes(address,password);
            return _context.ImportPrivateKey(key);

        }

        private byte[] RetrieveKeyBytes(string address, string password)
        {
            var service = new KeyStoreService();
            return service.DecryptKeyStoreFromFile(password, address);
            
        }
        
    }
}