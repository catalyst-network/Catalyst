namespace Catalyst.Node.Common.Interfaces
{
    public interface IKeystore
    {
        void StoreKey(IPrivateKey key, string password);
        
        void StoreKey(byte[] privateKey, string address, string password);
        
        IPrivateKey RetrieveKey(string address, string password);
        
        byte[] RetrieveKeyBytes(string address, string password);
    }
}