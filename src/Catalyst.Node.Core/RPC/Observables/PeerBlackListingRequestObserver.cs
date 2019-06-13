#region LICENSE

/**
* Copyright (c) 2019 Catalyst Network
*
* This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
*
* Catalyst.Node is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 2 of the License, or
* (at your option) any later version.
*
* Catalyst.Node is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
*/

#endregion

using System.Linq;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Google.Protobuf;
using Nethereum.RLP;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    public sealed class PeerBlackListingRequestObserver
        : RequestMessageObserverBase<SetPeerBlackListRequest>,
            IRpcRequestMessageObserver
    {
        /// <summary>
        /// The PeerBlackListingRequestHandler 
        /// </summary>
        private readonly IRepository<Peer> _peerRepository;
        
        public PeerBlackListingRequestObserver(IPeerIdentifier peerIdentifier,
            ILogger logger,
            IRepository<Peer> peerRepository)
            : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>
        /// Handlers the specified message.
        /// </summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("received message of type PeerBlackListingRequest");
            
            var deserialised = messageDto.Payload.FromProtocolMessage<SetPeerBlackListRequest>();
            var publicKey = deserialised.PublicKey.ToStringUtf8(); 
            var ip = deserialised.Ip.ToStringUtf8();
            var blackList = deserialised.Blacklist;

            var peerItem = _peerRepository.GetAll().FirstOrDefault(m => m.PeerIdentifier.Ip.ToString() == ip.ToString() 
             && m.PeerIdentifier.PublicKey.ToStringFromRLPDecoded() == publicKey);

            if (peerItem != null)
            {
                return new SetPeerBlackListResponse
                {
                    Blacklist = blackList,
                    Ip = deserialised.Ip,
                    PublicKey = deserialised.PublicKey
                };
            } //@TODO clean this up
            else
            {
                return new SetPeerBlackListResponse
                {
                    Blacklist = false,
                    Ip = string.Empty.ToUtf8ByteString(),
                    PublicKey = string.Empty.ToUtf8ByteString()
                };
            }
        }
    }
}


