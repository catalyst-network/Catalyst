using System.Net;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.P2P;
using Catalyst.Common.Util;

namespace Catalyst.Simulator
{
    public sealed class SimulationNode
    {
        public string Ip { get; set; }

        public int Port { get; set; }

        public string PublicKey { get; set; }

        public IPeerIdentifier ToPeerIdentifier()
        {
            return new PeerIdentifier(PublicKey.KeyToBytes(), IPAddress.Parse(Ip), Port);
        }
    }
}
