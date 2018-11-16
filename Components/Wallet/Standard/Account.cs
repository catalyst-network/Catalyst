namespace ADL.Wallet.Standard
{
    /*
     * An account is made up of an encrypted private key and a corresponding hashed public key
     */
    internal class Account
    {
        public byte[] PrivateKeyEncrypted { get; set; }
        public byte[] PublicKeyHash { get; set; }
    }
}