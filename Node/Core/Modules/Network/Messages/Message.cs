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

        public Message(IPEndPoint endPoint, IMessage message, byte network, byte version, byte type)
        {
            if (endPoint != null) EndPoint = endPoint;
            if (message != null) ProtoMessage = message;
            MessageDescriptor = BuildMsgDescriptor(network, version, type);
        }
        
        /// <summary>
        /// Builds a p2p message header
        /// @TODO we need to make sure header byte length is always same, do we want to check version is 4 bytes long now or when we load settings?
        /// </summary>
        /// <param name="network"></param>
        /// <param name="version"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static byte[] BuildMsgDescriptor(byte network, byte version, byte type)
        {
            return new []{network, version, type};
        }
    }
}
