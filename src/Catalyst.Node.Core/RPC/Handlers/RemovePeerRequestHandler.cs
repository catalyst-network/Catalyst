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
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Common.Rpc;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Handlers
{
    /// <summary>
    /// Remove Peer handler
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{RemovePeerRequest, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcRequestHandler" />
    public sealed class RemovePeerRequestHandler
        : CorrelatableMessageHandlerBase<RemovePeerRequest, IMessageCorrelationCache>,
            IRpcRequestHandler
    {
        /// <summary>The peer identifier</summary>
        private readonly IPeerIdentifier _peerIdentifier;

        /// <summary>The peer discovery</summary>
        private readonly IPeerDiscovery _peerDiscovery;

        /// <summary>The RPC message factory</summary>
        private readonly IRpcMessageFactory _rpcMessageFactory;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestHandler"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerDiscovery">The peer discovery.</param>
        /// <param name="messageCorrelationCache">The message correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcMessageFactory"></param>
        public RemovePeerRequestHandler(IPeerIdentifier peerIdentifier,
            IPeerDiscovery peerDiscovery,
            IRpcCorrelationCache messageCorrelationCache,
            ILogger logger,
            IRpcMessageFactory rpcMessageFactory) : base(messageCorrelationCache, logger)
        {
            _peerIdentifier = peerIdentifier;
            _peerDiscovery = peerDiscovery;
            _rpcMessageFactory = rpcMessageFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull();

            Logger.Debug("Received message of type RemovePeerRequest");
            try
            {
                UInt32 peerDeletedCount = 0;

                var deserialised = message.Payload.FromAnySigned<RemovePeerRequest>();
                var publicKeyIsEmpty = deserialised.PublicKey.IsEmpty;

                Guard.Argument(deserialised).NotNull();
                
                var peersToDelete = _peerDiscovery.PeerRepository.GetAll().TakeWhile(peer =>
                    peer.PeerIdentifier.Ip.To16Bytes().SequenceEqual(deserialised.PeerIp.ToByteArray()) &&
                    (publicKeyIsEmpty || peer.PeerIdentifier.PublicKey.SequenceEqual(deserialised.PublicKey.ToByteArray()))).ToArray();
                
                if (peersToDelete.Length > 0)
                {
                    foreach (var peerToDelete in peersToDelete)
                    {
                        _peerDiscovery.PeerRepository.Delete(peerToDelete);
                        peerDeletedCount += 1;
                    }
                }

                var removePeerResponse = new RemovePeerResponse
                {
                    DeletedCount = peerDeletedCount
                };

                var removePeerMessage = _rpcMessageFactory.GetMessage(new MessageDto(
                        removePeerResponse,
                        MessageTypes.Tell,
                        new PeerIdentifier(message.Payload.PeerId),
                        _peerIdentifier),
                    message.Payload.CorrelationId.ToGuid()
                );

                message.Context.Channel.WriteAndFlushAsync(removePeerMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoRequest after receiving message {0}", message);
                throw;
            }
        }
    }
}
