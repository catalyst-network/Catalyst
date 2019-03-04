using System.IO;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.KeyStore;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class Keystore
    {
        public void CreateStore()
        {
            var password = "monkey123445";
            
            var service = new KeyStoreService();
            var result = service.EncryptAndGenerateKeyStoreAsJson(password, privateKey, genAddress);

            fileName = service.GenerateUTCFileName(genAddress);

            using (var newfile = File.CreateText(fileName))
            {            
                newfile.Write(result);
                newfile.Flush();
            }
        }
    }
}