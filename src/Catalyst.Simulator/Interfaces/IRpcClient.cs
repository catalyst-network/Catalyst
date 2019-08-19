using System;
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.P2P;
using Google.Protobuf;

namespace Catalyst.Simulator.Interfaces
{
    public interface IRpcClient
    {
        Task<bool> ConnectRetryAsync(IPeerIdentifier peerIdentifier, int retryAttempts = 5);
        Task<bool> ConnectAsync(IPeerIdentifier peerIdentifier);
        void SendMessage<T>(T message) where T : IMessage;
        void ReceiveMessage<T>(Action<T> message) where T : IMessage<T>;
        bool IsConnected();
    }
}
