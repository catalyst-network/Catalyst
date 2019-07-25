using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;

namespace Catalyst.Common.Interfaces.P2P.Discovery
{
    public interface INeighbour
    {
        NeighbourState State { get; set; }
        IPeerIdentifier PeerIdentifier { get; }
        ICorrelationId DiscoveryPingCorrelationId { get; }
    }
}
