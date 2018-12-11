using System;
using ADL.Protocol.Peer;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Type = Google.Protobuf.WellKnownTypes.Type;

namespace ADL.Node.Core.Modules.Peer.Messages
{
    public class ResponseMessageFactory
    {
        public static dynamic GetMessage(int id)
        {
            switch (id)
            {
                case 1:
//                    return new PeerProtocol.Types.PingResponse();
                    return false;
                case 2:
                    return new PeerProtocol.Types.ChallengeResponse();
                case 3:
                    return new PeerProtocol.Types.PeerInfoResponse();
                case 4:
                    return new PeerProtocol.Types.PeerNeighborsResponse();
                default:
//                    return new PeerProtocol.Types.PingResponse();
                    return false;
            }            
        }
    }
}