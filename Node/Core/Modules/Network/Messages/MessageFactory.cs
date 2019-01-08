using System;
using ADL.Protocol.Peer;
using System.Collections.Generic;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Messages
{
    /// <summary>
    /// 
    /// </summary>
    public static class MessageFactory
    {
        // pass the request on the left hand side 🔥 🎵 💃 🕺 
        static Dictionary<int, int> RequestResponseIdPairings = new Dictionary<int, int>
        {
            {1, 2},
            {3, 4},
            {5, 6},
            {7, 8}
        };
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <param name="messageId"></param>
        /// <param name="message"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static Message Get(byte network, byte messageId, byte[] message, Connection connection)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (network <= 0) throw new ArgumentOutOfRangeException(nameof(network));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (messageId <= 0) throw new ArgumentOutOfRangeException(nameof(messageId));
            if (message.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(message));
            
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
