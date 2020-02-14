using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using System.Collections.Generic;

namespace Catalyst.Core.Modules.Sync.Manager
{
    public class Messenger : IMessenger
    {
        private IPeerClient _peerClient;
        private IPeerSettings _peerSettings;

        public Messenger(IPeerClient peerClient, IPeerSettings peerSettings)
        {
            _peerClient = peerClient;
            _peerSettings = peerSettings;
        }

        public void SendMessageToPeers(IMessage message, IEnumerable<PeerId> peers)
        {
            var protocolMessage = message.ToProtocolMessage(_peerSettings.PeerId);
            foreach (var peer in peers)
            {
                _peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer));
            }
        }
    }
}
