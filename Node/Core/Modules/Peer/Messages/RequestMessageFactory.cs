using System;
using ADL.Protocol.Peer;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Type = Google.Protobuf.WellKnownTypes.Type;

namespace ADL.Node.Core.Modules.Peer.Messages
{
    public class RequestMessageFactory
    {
        public static dynamic GetMessage(int id)
        {
            switch (id)
            {
                case 1:
                    return new PeerProtocol.Types.PingRequest();
                case 2:
                    return new PeerProtocol.Types.ChallengeRequest();
                case 3:
                    return new PeerProtocol.Types.PeerInfoRequest();
                case 4:
                    return new PeerProtocol.Types.PeerNeighborsRequest();
                default:
                    return new PeerProtocol.Types.PingRequest();
            }            
        }
    }
}