namespace Nethereum.KeyStore.JsonDeserialisation
{
    public class CryptoInfoDTOBase
    {
        public CryptoInfoDTOBase()
        {
            Cipherparams = new CipherParamsDTO();
        }

        public string? Cipher { get; set; }
        public string? CipherText { get; set; }
        public CipherParamsDTO Cipherparams { get; set; }
        public string? Kdf { get; set; }
        public string? Mac { get; set; }
    }
}
