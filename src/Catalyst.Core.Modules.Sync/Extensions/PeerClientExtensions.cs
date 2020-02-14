using Catalyst.Abstractions.P2P;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Messaging.Dto;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using System.Collections.Generic;

namespace Catalyst.Core.Modules.Sync.Extensions
{
    public static class PeerClientExtensions
    {
        public static void SendMessageToPeers(this IPeerClient peerClient, IPeerSettings peerSettings, IMessage message, IEnumerable<PeerId> peers)
        {
            var protocolMessage = message.ToProtocolMessage(peerSettings.PeerId);
            foreach (var peer in peers)
            {
                peerClient.SendMessage(new MessageDto(
                    protocolMessage,
                    peer));
            }
        }
    }
}
