using Catalyst.Protocol.Network;

namespace Catalyst.Abstractions.Config
{
    public interface INetworkTypeProvider
    {
        NetworkType NetworkType { get; }
    }
}
