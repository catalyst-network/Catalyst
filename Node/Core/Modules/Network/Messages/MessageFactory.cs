using ADL.Protocol.Peer;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public class MessageFactory
    {
        public static dynamic Get(int id)
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
                case 5:
                    return new PeerProtocol.Types.ChallengeResponse();
                case 6:
                    return new PeerProtocol.Types.PeerInfoResponse();
                case 7:
                    return new PeerProtocol.Types.PeerNeighborsResponse();
                default:
                    return new PeerProtocol.Types.PingRequest();
            }            
        }
    }
}