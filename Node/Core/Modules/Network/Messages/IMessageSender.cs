using System.Net;

namespace ADL.Node.Core.Modules.Network.Messages
{
    public interface IMessageSender
    {
        void Send(IPEndPoint endPoint, byte[] message);
    }
}
