using Google.Protobuf;
using ADL.Node.Core.Modules.Network.Connections;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public class Message
    {
        internal Connection Connection { get; set; }
        internal IMessage ProtoMessage { get; set; }
        private byte[] MessageDescriptor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <param name="messageDescriptor"></param>
        public Message(Connection connection, IMessage message, byte[] messageDescriptor)
        {
            if (connection != null) Connection = connection;
            if (message != null) ProtoMessage = message;
            MessageDescriptor = messageDescriptor;
        }
        
        /// <summary>
        /// 
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
        
        /// <summary>
        /// Message descriptor should return a 2byte array
        /// The first byte denotes the network 0 = devNet, 1 = testNet, 2 = liveNet
        /// The second byte is the message type
        /// </summary>
        /// <param name="network"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static byte[] BuildMsgDescriptor(byte network, byte type)
        {
            return new [] { network, type };
        }
    }
}
