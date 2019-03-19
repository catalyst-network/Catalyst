using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Cli
{

    public class CatalystCli : ICatalystCli
    {
        public IAds Ads { get; set; }
        public CatalystCli(IAds ads) { Ads = ads; }
    }
}