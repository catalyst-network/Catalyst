namespace Catalyst.Node.Common.Helpers.KeyStore
{
    public interface IKeyStoreWrapper
    {
        string GetAddressFromKeyStore(string json);
        string GenerateUTCFileName(string address);
        byte[] DecryptKeyStoreFromFile(string password, string filePath);
        byte[] DecryptKeyStoreFromJson(string password, string json);
        string EncryptAndGenerateDefaultKeyStoreAsJson(string password, byte[] key, string address);
    }
}
