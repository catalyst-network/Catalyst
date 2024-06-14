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
                Address = scryptKeyStore.Address,
                Id = scryptKeyStore.Id,
                Version = scryptKeyStore.Version
            };
            if (scryptKeyStore.Crypto != null)
            {
                dto.crypto.Cipher = scryptKeyStore.Crypto.Cipher;
                dto.crypto.CipherText = scryptKeyStore.Crypto.CipherText;
                dto.crypto.Kdf = scryptKeyStore.Crypto.Kdf;
                dto.crypto.Mac = scryptKeyStore.Crypto.Mac;
                dto.crypto.kdfparams.r = scryptKeyStore.Crypto.Kdfparams != null ? scryptKeyStore.Crypto.Kdfparams.R : 0;
                dto.crypto.kdfparams.n = scryptKeyStore.Crypto.Kdfparams != null ? scryptKeyStore.Crypto.Kdfparams.N : 0;
                dto.crypto.kdfparams.p = scryptKeyStore.Crypto.Kdfparams != null ? scryptKeyStore.Crypto.Kdfparams.P : 0;
                dto.crypto.kdfparams.Dklen = scryptKeyStore.Crypto.Kdfparams != null ? scryptKeyStore.Crypto.Kdfparams.Dklen : 0;
                dto.crypto.kdfparams.Salt = scryptKeyStore.Crypto.Kdfparams != null ? scryptKeyStore.Crypto.Kdfparams.Salt : string.Empty;
                dto.crypto.Cipherparams.Iv = scryptKeyStore.Crypto.CipherParams != null ? scryptKeyStore.Crypto.CipherParams.Iv : string.Empty;
            }
            return dto;
        }

        public static KeyStore<ScryptParams> MapDTOToModel(KeyStoreScryptDTO? dto)
        {
            var scryptKeyStore = new KeyStore<ScryptParams>();
            if (dto != null)
            {
                scryptKeyStore.Address = dto.Address ?? string.Empty;
                scryptKeyStore.Id = dto.Id ?? string.Empty;
                scryptKeyStore.Version = dto.Version;
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
                scryptKeyStore.Crypto.Kdfparams.Dklen = dto.crypto.kdfparams.Dklen;
                scryptKeyStore.Crypto.Kdfparams.Salt = dto.crypto.kdfparams.Salt ?? string.Empty;
                scryptKeyStore.Crypto.CipherParams = new CipherParams()
                {
                    Iv = dto.crypto.Cipherparams.Iv ?? string.Empty
                };
            }
            return scryptKeyStore;
        }
    }
}
