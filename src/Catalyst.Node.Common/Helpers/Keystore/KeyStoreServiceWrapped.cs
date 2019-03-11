using Nethereum.KeyStore;

namespace Catalyst.Node.Common.Helpers.KeyStore {
    public class KeyStoreServiceWrapped : IKeyStoreWrapper
    {
        private readonly KeyStoreService _keyStoreWrapperImplementation;
        public KeyStoreServiceWrapped()
        {
            _keyStoreWrapperImplementation = new KeyStoreService();
        }
        public string GetAddressFromKeyStore(string json)
        { return _keyStoreWrapperImplementation.GetAddressFromKeyStore(json); }
        public string GenerateUTCFileName(string address)
        { return _keyStoreWrapperImplementation.GenerateUTCFileName(address); }
        public byte[] DecryptKeyStoreFromFile(string password, string filePath)
        { return _keyStoreWrapperImplementation.DecryptKeyStoreFromFile(password, filePath); }
        public byte[] DecryptKeyStoreFromJson(string password, string json)
        { return _keyStoreWrapperImplementation.DecryptKeyStoreFromJson(password, json); }
        public string EncryptAndGenerateDefaultKeyStoreAsJson(string password, byte[] key, string address)
        { return _keyStoreWrapperImplementation.EncryptAndGenerateDefaultKeyStoreAsJson(password, key, address); }
    }
}