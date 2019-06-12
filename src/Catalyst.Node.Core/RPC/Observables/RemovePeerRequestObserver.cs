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

using System;
using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    /// Remove Peer handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class RemovePeerRequestObserver
        : ObserverBase<RemovePeerRequest>,
            IRpcRequestObserver
    {
        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The peer discovery</summary>
        private readonly IRepository<Peer> _peerRepository;

        /// <summary>The RPC message factory</summary>
        private readonly IMessageFactory _messageFactory;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestObserver"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="messageFactory"></param>
        public RemovePeerRequestObserver(IPeerIdentifier peerIdentifier,
            IRepository<Peer> peerRepository,
            ILogger logger,
            IMessageFactory messageFactory) : base(logger)
        {
            _peerIdentifier = peerIdentifier;
            _peerRepository = peerRepository;
            _messageFactory = messageFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        protected override void Handler(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto).NotNull();

            Logger.Debug("Received message of type RemovePeerRequest");
            try
            {
                UInt32 peerDeletedCount = 0;

                var deserialised = messageDto.Payload.FromProtocolMessage<RemovePeerRequest>();
                var publicKeyIsEmpty = deserialised.PublicKey.IsEmpty;

                Guard.Argument(deserialised).NotNull();
                
                var peersToDelete = _peerRepository.GetAll().TakeWhile(peer =>
                    Ip.To16Bytes(peer.PeerIdentifier.Ip).SequenceEqual(deserialised.PeerIp.ToByteArray()) &&
                    (publicKeyIsEmpty || peer.PeerIdentifier.PublicKey.SequenceEqual(deserialised.PublicKey.ToByteArray()))).ToArray();
                
                if (peersToDelete.Length > 0)
                {
                    foreach (var peerToDelete in peersToDelete)
                    {
                        _peerRepository.Delete(peerToDelete);
                        peerDeletedCount += 1;
                    }
                }

                var removePeerResponse = new RemovePeerResponse
                {
                    DeletedCount = peerDeletedCount
                };

                var removePeerMessage = _messageFactory.GetMessage(new MessageDto(
                        removePeerResponse,
                        MessageTypes.Response,
                        new PeerIdentifier(messageDto.Payload.PeerId),
                        _peerIdentifier),
                    messageDto.Payload.CorrelationId.ToGuid()
                );

                messageDto.Context.Channel.WriteAndFlushAsync(removePeerMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", messageDto);
                throw;
            }
        }
    }
}
