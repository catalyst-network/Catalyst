using Nethereum.KeyStore.Model;
using Newtonsoft.Json;

namespace Nethereum.KeyStore.JsonDeserialisation
{
    public class JsonKeyStoreScryptSerialiser
    {
        public static string SerialiseScrypt(KeyStore<ScryptParams> scryptKeyStore)
        {
            return JsonConvert.SerializeObject(scryptKeyStore);
        }

        public static KeyStore<ScryptParams> DeserialiseScrypt(string json)
        {
            var dto = JsonConvert.DeserializeObject<KeyStoreScryptDTO>(json);
            return MapDTOToModel(dto);
        }

        public static KeyStoreScryptDTO MapModelToDTO(KeyStore<ScryptParams> scryptKeyStore)
        {
            var dto = new KeyStoreScryptDTO()
            {
                address = scryptKeyStore.Address,
                id = scryptKeyStore.Id,
                version = scryptKeyStore.Version
            };
            dto.crypto.Cipher = scryptKeyStore.Crypto.Cipher;
            dto.crypto.CipherText = scryptKeyStore.Crypto.CipherText;
            dto.crypto.Kdf = scryptKeyStore.Crypto.Kdf;
            dto.crypto.Mac = scryptKeyStore.Crypto.Mac;
            dto.crypto.kdfparams.r = scryptKeyStore.Crypto.Kdfparams.R;
            dto.crypto.kdfparams.n = scryptKeyStore.Crypto.Kdfparams.N;
            dto.crypto.kdfparams.p = scryptKeyStore.Crypto.Kdfparams.P;
            dto.crypto.kdfparams.dklen = scryptKeyStore.Crypto.Kdfparams.Dklen;
            dto.crypto.kdfparams.salt = scryptKeyStore.Crypto.Kdfparams.Salt;
            dto.crypto.Cipherparams.Iv = scryptKeyStore.Crypto.CipherParams.Iv;
            return dto;
        }

        public static KeyStore<ScryptParams> MapDTOToModel(KeyStoreScryptDTO? dto)
        {
            var scryptKeyStore = new KeyStore<ScryptParams>();
            if (dto != null)
            {
                scryptKeyStore.Address = dto.address;
                scryptKeyStore.Id = dto.id;
                scryptKeyStore.Version = dto.version;
                scryptKeyStore.Crypto = new CryptoInfo<ScryptParams>
                {
                    Cipher = dto.crypto.Cipher,
                    CipherText = dto.crypto.CipherText,
                    Kdf = dto.crypto.Kdf,
                    Mac = dto.crypto.Mac,
                    Kdfparams = new ScryptParams()
                };
                scryptKeyStore.Crypto.Kdfparams.R = dto.crypto.kdfparams.r;
                scryptKeyStore.Crypto.Kdfparams.N = dto.crypto.kdfparams.n;
                scryptKeyStore.Crypto.Kdfparams.P = dto.crypto.kdfparams.p;
                scryptKeyStore.Crypto.Kdfparams.Dklen = dto.crypto.kdfparams.dklen;
                scryptKeyStore.Crypto.Kdfparams.Salt = dto.crypto.kdfparams.salt;
                scryptKeyStore.Crypto.CipherParams = new CipherParams()
                {
                    Iv = dto.crypto.Cipherparams.Iv ?? string.Empty
                };
            }
            return scryptKeyStore;
        }
    }
}
