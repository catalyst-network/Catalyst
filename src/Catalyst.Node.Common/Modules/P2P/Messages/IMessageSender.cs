using Google.Protobuf;

namespace Catalyst.Node.Common.Modules.P2P.Messages
{
    public interface IMessageSender
    {
        /// <summary>
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        void Send(IConnection connection, IMessage message);
    }
}