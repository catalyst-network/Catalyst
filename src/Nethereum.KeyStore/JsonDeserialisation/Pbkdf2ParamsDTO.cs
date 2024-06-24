namespace Nethereum.KeyStore.JsonDeserialisation
{
    public class Pbkdf2ParamsDTO : KdfParamsDTO
    {
        public int C { get; set; }
        public string? Prf { get; set; }
    }
}
