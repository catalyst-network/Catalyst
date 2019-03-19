using Catalyst.Node.Common.Interfaces;

namespace Catalyst.Cli
{
    public interface ICatalystCli { }

    public class CatalystCli : ICatalystCli
    {
        private readonly IAds _ads;
        public CatalystCli(IAds ads) { _ads = ads; }
    }
}