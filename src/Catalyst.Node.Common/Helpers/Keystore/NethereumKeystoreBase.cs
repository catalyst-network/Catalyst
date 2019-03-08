using Nethereum.KeyStore;
using System.IO;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class NethereumKeystoreBase
    {
        public void StoreKey(byte[] privateKey, string address, string password)
        {
            var service = new KeyStoreService();
            var json = service.EncryptAndGenerateDefaultKeyStoreAsJson(password, privateKey, address);

            using (var newfile = File.CreateText(address))
            {            
                newfile.Write(json);
                newfile.Flush();
            }
        }
        
        public byte[] RetrieveKeyBytes(string address, string password)
        {
            var service = new KeyStoreService();
            return service.DecryptKeyStoreFromFile(password, address);
            
        }
    }
}