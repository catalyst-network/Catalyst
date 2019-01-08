using ADL.Protocol.Peer;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public static class MessageFactory
    {
        public static Message Get(byte network, byte messageId, byte[] message, Connection connection)
        {
            switch (messageId)
            {
                case 1:
                    return new Message(connection, new PeerProtocol.Types.PingRequest(), network, messageId);
                case 2:
                    return new Message(connection, new PeerProtocol.Types.ChallengeRequest(), network, messageId);
                case 3:
                    return new Message(connection, new PeerProtocol.Types.PeerInfoRequest(), network, messageId);
                case 4:
                    return new Message(connection, new PeerProtocol.Types.PeerNeighborsRequest(), network, messageId);
                case 5:
                    return new Message(connection, new PeerProtocol.Types.ChallengeResponse(), network, messageId);
                case 6:
                    return new Message(connection, new PeerProtocol.Types.PeerInfoResponse(), network, messageId);
                case 7:
                    return new Message(connection, new PeerProtocol.Types.PeerNeighborsResponse(), network, messageId);
                default:
                    return new Message(connection, new PeerProtocol.Types.PingRequest(), network, messageId);
            }            
        }
    }
}