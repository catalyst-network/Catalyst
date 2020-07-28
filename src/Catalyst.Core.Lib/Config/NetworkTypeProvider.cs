using Catalyst.Abstractions.Config;
using Catalyst.Protocol.Network;

namespace Catalyst.Core.Lib.Config
{
    public class NetworkTypeProvider : INetworkTypeProvider
    {
        public NetworkType NetworkType { get; }

        public NetworkTypeProvider(NetworkType networkType)
        {
            NetworkType = networkType;
        }
    }
}
