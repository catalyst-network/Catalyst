using System.IO;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.KeyStore;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class Keystore
    {
        
        public void CreateKeystoreFile(string password, byte[] privateKey, string address)
        {
            
            
            var service = new KeyStoreService();
            //var json = service.EncryptAndGenerateKeyStoreAsJson(password, privateKey, address);
            var json = service.EncryptAndGenerateDefaultKeyStoreAsJson(password, privateKey, address);

            var fileName = service.GenerateUTCFileName(address);

            using (var newfile = File.CreateText(fileName))
            {            
                newfile.Write(json);
                newfile.Flush();
            }
        }
        
    }
}