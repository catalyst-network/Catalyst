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
                address = pbdk2KeyStore.Address,
                id = pbdk2KeyStore.Id,
                version = pbdk2KeyStore.Version
            };
            dto.crypto.cipher = pbdk2KeyStore.Crypto.Cipher;
            dto.crypto.cipherText = pbdk2KeyStore.Crypto.CipherText;
            dto.crypto.kdf = pbdk2KeyStore.Crypto.Kdf;
            dto.crypto.mac = pbdk2KeyStore.Crypto.Mac;
            dto.crypto.kdfparams.c = pbdk2KeyStore.Crypto.Kdfparams.Count;
            dto.crypto.kdfparams.prf = pbdk2KeyStore.Crypto.Kdfparams.Prf;
            dto.crypto.kdfparams.dklen = pbdk2KeyStore.Crypto.Kdfparams.Dklen;
            dto.crypto.kdfparams.salt = pbdk2KeyStore.Crypto.Kdfparams.Salt;
            dto.crypto.cipherparams.Iv = pbdk2KeyStore.Crypto.CipherParams.Iv;
            return dto;
        }

        public static KeyStore<Pbkdf2Params> MapDTOToModel(KeyStorePbkdf2DTO? dto)
        {
            var pbdk2KeyStore = new KeyStore<Pbkdf2Params>();
            if (dto != null)
            {
                pbdk2KeyStore.Address = dto.address;
                pbdk2KeyStore.Id = dto.id;
                pbdk2KeyStore.Version = dto.version;
                pbdk2KeyStore.Crypto = new CryptoInfo<Pbkdf2Params>()
                {
                    Cipher = dto.crypto.cipher,
                    CipherText = dto.crypto.cipherText,
                    Kdf = dto.crypto.kdf,
                    Mac = dto.crypto.mac,
                    Kdfparams = new Pbkdf2Params()
                };
                pbdk2KeyStore.Crypto.Kdfparams.Count = dto.crypto.kdfparams.c;
                pbdk2KeyStore.Crypto.Kdfparams.Prf = dto.crypto.kdfparams.prf;
                pbdk2KeyStore.Crypto.Kdfparams.Dklen = dto.crypto.kdfparams.dklen;
                pbdk2KeyStore.Crypto.Kdfparams.Salt = dto.crypto.kdfparams.salt;
                pbdk2KeyStore.Crypto.CipherParams = new CipherParams()
                {
                    Iv = dto.crypto.cipherparams?.Iv != null ? dto.crypto.cipherparams.Iv : string.Empty
                };
            }
            return pbdk2KeyStore;
        }
    }
}
