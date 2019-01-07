using ADL.Hex.HexConvertors.Extensions;
using ADL.KeyStore.Model;
using Newtonsoft.Json;

namespace ADL.KeyStore.JsonDeserialisation
{
    public class CryptoInfoPbkdf2DTO : CryptoInfoDTOBase
    {
        public CryptoInfoPbkdf2DTO()
        {
            kdfparams = new Pbkdf2ParamsDTO();
        }

        public Pbkdf2ParamsDTO kdfparams { get; set; }
    }
}