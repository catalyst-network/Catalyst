
using System.Collections.Generic;
using ADL.Node.Core.Modules.Network.Peer;

namespace ADL.Node.Core.Modules.Network
{
    public interface IKademlia
    {
        bool Ping();
        bool Store(string k, byte[] v);
        List<PeerIdentifier> FindNode();
        
    }
}