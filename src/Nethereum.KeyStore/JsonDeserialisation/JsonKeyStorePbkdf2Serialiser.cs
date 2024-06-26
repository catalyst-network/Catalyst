using Nethereum.KeyStore.Model;
using Newtonsoft.Json;

namespace Nethereum.KeyStore.JsonDeserialisation
{
    public class JsonKeyStorePbkdf2Serialiser
    {
        public static string SerialisePbkdf2(KeyStore<Pbkdf2Params> pbdk2KeyStore)
        {
            var dto = MapModelToDTO(pbdk2KeyStore);
            return JsonConvert.SerializeObject(pbdk2KeyStore);
        }

        public static KeyStore<Pbkdf2Params> DeserialisePbkdf2(string json)
        {
            var dto = JsonConvert.DeserializeObject<KeyStorePbkdf2DTO>(json);
            return MapDTOToModel(dto);
        }

        public static KeyStorePbkdf2DTO MapModelToDTO(KeyStore<Pbkdf2Params> pbdk2KeyStore)
        {
            var dto = new KeyStorePbkdf2DTO()
            {
                Address = pbdk2KeyStore.Address,
                Id = pbdk2KeyStore.Id,
                Version = pbdk2KeyStore.Version
            };
            dto.crypto.Cipher = pbdk2KeyStore.Crypto.Cipher;
            dto.crypto.CipherText = pbdk2KeyStore.Crypto.CipherText;
            dto.crypto.Kdf = pbdk2KeyStore.Crypto.Kdf;
            dto.crypto.Mac = pbdk2KeyStore.Crypto.Mac;
            dto.crypto.kdfparams.C = pbdk2KeyStore.Crypto.Kdfparams != null ? pbdk2KeyStore.Crypto.Kdfparams.Count : 0;
            dto.crypto.kdfparams.Prf = pbdk2KeyStore.Crypto.Kdfparams != null ? pbdk2KeyStore.Crypto.Kdfparams.Prf : string.Empty;
            dto.crypto.kdfparams.Dklen = pbdk2KeyStore.Crypto.Kdfparams != null ? pbdk2KeyStore.Crypto.Kdfparams.Dklen : 0;
            dto.crypto.kdfparams.Salt = pbdk2KeyStore.Crypto.Kdfparams != null ? pbdk2KeyStore.Crypto.Kdfparams.Salt : string.Empty;
            dto.crypto.Cipherparams.Iv = pbdk2KeyStore.Crypto.CipherParams != null ? pbdk2KeyStore.Crypto.CipherParams.Iv : string.Empty;
            return dto;
        }

        public static KeyStore<Pbkdf2Params> MapDTOToModel(KeyStorePbkdf2DTO? dto)
        {
            var pbdk2KeyStore = new KeyStore<Pbkdf2Params>();
            if (dto != null)
            {
                pbdk2KeyStore.Address = dto.Address != null ? dto.Address : string.Empty;
                pbdk2KeyStore.Id = dto.Address != null ? dto.Address : string.Empty;
                pbdk2KeyStore.Version = dto.Address != null ? dto.Version : 0;
                pbdk2KeyStore.Crypto = new CryptoInfo<Pbkdf2Params>()
                {
                    Cipher = dto.crypto.Cipher,
                    CipherText = dto.crypto.CipherText,
                    Kdf = dto.crypto.Kdf,
                    Mac = dto.crypto.Mac,
                    Kdfparams = new Pbkdf2Params()
                };
                pbdk2KeyStore.Crypto.Kdfparams.Count = dto.crypto.kdfparams.C;
                pbdk2KeyStore.Crypto.Kdfparams.Prf = dto.crypto.kdfparams.Prf;
                pbdk2KeyStore.Crypto.Kdfparams.Dklen = dto.crypto.kdfparams.Dklen;
                pbdk2KeyStore.Crypto.Kdfparams.Salt = dto.crypto.kdfparams != null && dto.crypto.kdfparams.Salt != null ? dto.crypto.kdfparams.Salt : string.Empty;
                pbdk2KeyStore.Crypto.CipherParams = new CipherParams()
                {
                    Iv = dto.crypto.Cipherparams?.Iv != null ? dto.crypto.Cipherparams.Iv : string.Empty
                };
            }
            return pbdk2KeyStore;
        }
    }
}
