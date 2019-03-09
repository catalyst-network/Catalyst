namespace Catalyst.Node.Common.Interfaces {
    public interface IKeyStore {
        ICryptoContext CryptoContext { get; }
        IPrivateKey GetKey(IPublicKey publicKey, string password);
        IPrivateKey GetKey(string address, string password);
        bool StoreKey(IPrivateKey privateKey, string address, string password);
    }
}