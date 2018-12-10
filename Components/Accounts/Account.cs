using System;
using System.Numerics;
using ADL.KeySigner;
using ADL.KeyStore;
//using ADL.RPC.Accounts;
//using ADL.RPC.NonceServices;
//using ADL.RPC.TransactionManagers;
using ADL.KeySigner;

namespace ADL.Accounts
{
//    : IAccount
    public class Account
    {
        public BigInteger? ChainId { get; }

#if !PCL
        public static Account LoadFromKeyStoreFile(string filePath, string password)
        {
            var keyStoreService = new ADL.KeyStore.KeyStoreService();
            var key = keyStoreService.DecryptKeyStoreFromFile(password, filePath);
            return new Account(key);
        }
#endif
        public static Account LoadFromKeyStore(string json, string password, BigInteger? chainId = null)
        {
            var keyStoreService = new KeyStoreService();
            var key = keyStoreService.DecryptKeyStoreFromJson(password, json);
            return new Account(key, chainId);
        }

        public string PrivateKey { get; private set; }

        public Account(AtlasECKey key, BigInteger? chainId = null)
        {
            ChainId = chainId;
            Initialise(key);
        }

        public Account(string privateKey, BigInteger? chainId = null)
        {
            ChainId = chainId;
            Initialise(new AtlasECKey(privateKey));
        }

        public Account(byte[] privateKey, BigInteger? chainId = null)
        {
            ChainId = chainId;
            Initialise(new AtlasECKey(privateKey, true));
        }

        public Account(AtlasECKey key, Chain chain) : this(key, (int) chain)
        {
        }

        public Account(string privateKey, Chain chain) : this(privateKey, (int) chain)
        {
        }

        public Account(byte[] privateKey, Chain chain) : this(privateKey, (int) chain)
        {
        }

        private void Initialise(AtlasECKey key)
        {
            PrivateKey = key.GetPrivateKey();
            Address = key.GetPublicAddress();
            InitialiseDefaultTransactionManager();
        }

        protected virtual void InitialiseDefaultTransactionManager()
        {
            throw new NotImplementedException();
//            TransactionManager = new AccountSignerTransactionManager(null, this, ChainId);
        }

        public string Address { get; protected set; }
//        public ITransactionManager TransactionManager { get; protected set; }
//        public INonceService NonceService { get; set; }
    }
}