using System;
using ADL.Protocol.Peer;
using System.Collections.Generic;
using ADL.Node.Core.Modules.Network.Connections;
using Google.Protobuf;

namespace ADL.Node.Core.Modules.Network.Messages
{
    /// <summary>
    /// 
    /// </summary>
    public static class MessageFactory
    {
        // pass the request on the left hand side 🔥 🎵 💃 🕺 ;)
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
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Message RequestFactory(byte network, byte messageId, Connection connection, IMessage message)
        {
            if (network <= 0) throw new ArgumentOutOfRangeException(nameof(network));
            if (connection == null) throw new ArgumentNullException(nameof(connection));
            if (messageId <= 0) throw new ArgumentOutOfRangeException(nameof(messageId));
            
            return new Message(connection, message, network, messageId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="network"></param>
        /// <param name="messageId"></param>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Message ResponseFactory(byte network, byte messageId, Connection connection, byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}
