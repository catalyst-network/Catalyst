using Newtonsoft.Json;

namespace ADL.KeyStore.Model
{
    public class KdfParams
    {
        [JsonProperty("dklen")]
        public int Dklen { get; set; }

        [JsonProperty("salt")]
        public string Salt { get; set; }
    }
}