using Catalyst.Common.Interfaces.Cli.Options;
using CommandLine;

namespace Catalyst.Cli.Options
{
    public class OptionsBase : IOptionsBase
    {
        /// <inheritdoc />
        [Option('n', "node", Required = true, HelpText = "A valid node ID as listed in the nodes.json config file.")]
        public string Node { get; set; }
    }
}
