using ADL.Helpers.Math;

namespace ADL.Wallet.Standard
{
    /*
     * A UserWalletAccount consists of a keypair
     */
    internal class UserWalletAccount : WalletAccount
    {
        public KeyPair Key;

        public override bool HasKey
        {
            get { return Key != null; }
        }
        
        public UserWalletAccount(UInt160 scriptHash)
            : base(scriptHash)
        {
            
        }

        public override KeyPair GetKey()
        {
            return Key;
        }
    }
}