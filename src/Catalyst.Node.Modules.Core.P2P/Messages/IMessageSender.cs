using Catalyst.Node.Modules.Core.P2P.Connections;
using Google.Protobuf;

namespace Catalyst.Node.Modules.Core.P2P.Messages
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
