using ADL.Node.Core.Modules.Network.Connections;
using Google.Protobuf;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public interface IMessageSender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        void Send(Connection connection, IMessage message);
    }
}
