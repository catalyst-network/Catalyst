using Catalyst.Protocol.Network;

namespace Catalyst.Abstractions.Config
{
    public interface IConfigEditor
    {
        void RunConfigEditor(string dataDir, NetworkType networkType = NetworkType.Devnet);
    }
}
