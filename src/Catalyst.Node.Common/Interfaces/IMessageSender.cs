using Google.Protobuf;

namespace Catalyst.Node.Common.Interfaces
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