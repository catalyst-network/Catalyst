using System;
using System.Net;
using ADL.Protocol.Peer;
using Google.Protobuf;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public class Message
    {
        private IPEndPoint EndPoint { get; set; }
        private IMessage ProtoMessage { get; set; }
        private byte[] MessageDescriptor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="message"></param>
        /// <param name="messageDescriptor"></param>
        public Message(IPEndPoint endPoint, IMessage message, byte[] messageDescriptor)
        {
            if (endPoint != null) EndPoint = endPoint;
            if (message != null) ProtoMessage = message;
            MessageDescriptor = messageDescriptor;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="message"></param>
        /// <param name="network"></param>
        /// <param name="type"></param>
        public Message(IPEndPoint endPoint, IMessage message, byte network, byte type)
        {
            if (endPoint != null) EndPoint = endPoint;
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
