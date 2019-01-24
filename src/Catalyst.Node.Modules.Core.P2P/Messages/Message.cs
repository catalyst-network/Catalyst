using System;
using Catalyst.Helpers.IO;
using Google.Protobuf;

namespace Catalyst.Node.Modules.Core.P2P.Messages
{
    public class Message
    {
        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <param name="messageDescriptor"></param>
        public Message(Connection connection, IMessage message, byte[] messageDescriptor)
        {
            if (messageDescriptor == null) throw new ArgumentNullException(nameof(messageDescriptor));
            if (messageDescriptor.Length == 0)
                throw new ArgumentException("Value cannot be an empty collection.", nameof(messageDescriptor));
            if (message != null) ProtoMessage = message;
            if (connection != null) Connection = connection;
            MessageDescriptor = messageDescriptor;
        }

        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <param name="network"></param>
        /// <param name="type"></param>
        public Message(Connection connection, IMessage message, byte network, byte type)
        {
            if (connection != null) Connection = connection;
            if (message != null) ProtoMessage = message;
            MessageDescriptor = BuildMsgDescriptor(network, type);
        }

        internal Connection Connection { get; set; }
        internal IMessage ProtoMessage { get; set; }
        private byte[] MessageDescriptor { get; }

        /// <summary>
        ///     Message descriptor should return a 2byte array
        ///     The first byte denotes the network 0 = devNet, 1 = testNet, 2 = liveNet
        ///     The second byte is the message type
        /// </summary>
        /// <param name="network"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static byte[] BuildMsgDescriptor(byte network, byte type)
        {
            return new[] {network, type};
        }
    }
}