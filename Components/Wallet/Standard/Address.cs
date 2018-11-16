namespace ADL.Wallet.Standard
{
    /*
     * An Address is made up of a hash of the public key
     */
    internal class Address
    {
        public byte[] PublicKeyHash { get; set; }
    }
}