using Catalyst.Helpers.IO;
using Google.Protobuf;

namespace Catalyst.Node.Modules.Core.P2P.Messages
{
    public interface IMessageSender
    {
        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        void Send(Connection connection, IMessage message);
    }
}