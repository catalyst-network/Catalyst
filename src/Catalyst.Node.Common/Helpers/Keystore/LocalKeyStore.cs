using System;
using System.IO;
using Catalyst.Node.Common.Interfaces;
using Nethereum.KeyStore;
using Serilog;

namespace Catalyst.Node.Common.Helpers.Keystore
{
    public class LocalKeyStore : IKeyStore
    {
        private readonly ILogger _logger;
        private  ICryptoContext CryptoContext { get; }
        private KeyStoreService KeyStoreService { get; }
        private FileSystem.FileSystem FileSystem { get; }

        public LocalKeyStore(ICryptoContext cryptoContext, ILogger logger)
        {
            CryptoContext = cryptoContext;
            _logger = logger; 
            KeyStoreService = new KeyStoreService();
            FileSystem  = new FileSystem.FileSystem();
        }
        
        public IPrivateKey GetKey(IPublicKey publicKey, string password) { throw new System.NotImplementedException(); }

        public IPrivateKey GetKey(string address, string password)
        {
            return CryptoContext.ImportPrivateKey(KeyStoreService.DecryptKeyStoreFromFile(password, address));
        }

        public bool StoreKey(IPrivateKey privateKey, string address, string password)
        {
            var json = KeyStoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, CryptoContext.ExportPrivateKey(privateKey), address);

            try
            {
                using (var keyStoreFile = FileSystem.File.CreateText(address))
                {
                    keyStoreFile.Write(json);
                    keyStoreFile.Flush();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
                return false;
            }
            return true;
        }
    }
}
