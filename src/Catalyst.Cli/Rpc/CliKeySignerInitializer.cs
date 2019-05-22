using Catalyst.Common.Interfaces.P2P;
using Microsoft.Extensions.Configuration;
using System.IO;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.Cryptography;
using Catalyst.Common.Interfaces.KeyStore;
using Catalyst.Common.KeyStore;
using Catalyst.Common.Modules.KeySigner;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Catalyst.Cli.Rpc
{
    public class CliKeySignerInitializer : KeySignerInitializerBase
    {
        private readonly IConfigurationRoot _configuration;

        public CliKeySignerInitializer(IConfigurationRoot configuration,
            IPasswordReader passwordReader,
            IKeyStore keyStore,
            IUserOutput userOutput,
            ILogger logger) : base(passwordReader, keyStore, userOutput, logger)
        {
            _configuration = configuration;
        }

        public override void SetConfigurationValue(string publicKey)
        {
            var keyPath = Path.Combine(KeyStore.GetBaseDir(), Constants.ShellConfigFile);

            var jsonObject = JObject.Parse(File.ReadAllText(keyPath));
            jsonObject["CatalystCliConfig"]["PublicKey"] = publicKey;

            File.WriteAllText(keyPath, jsonObject.ToString(Formatting.Indented));
        }

        public override IPeerIdentifier GetPeerIdentifier() { return Commands.Commands.BuildCliPeerId(_configuration); }
    }
}
