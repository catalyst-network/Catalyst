using Newtonsoft.Json;

namespace Catalyst.Core.Modules.Dfs.Controllers.V0
{
    /// <summary>
    ///  A hash to some data.
    /// </summary>
    public class HashDto
    {
        /// <summary>
        ///   Typically a CID.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Hash;

        /// <summary>
        ///   An error message.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Error;
    }
}
