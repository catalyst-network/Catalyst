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
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.Network;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using SharpRepository.Repository;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    /// Remove Peer handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class RemovePeerRequestObserver
        : RequestObserverBase<RemovePeerRequest>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IRepository<Peer> _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="RemovePeerRequestObserver"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public RemovePeerRequestObserver(IPeerIdentifier peerIdentifier,
            IRepository<Peer> peerRepository,
            ILogger logger) : base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("Received message of type RemovePeerRequest");

            Guard.Argument(messageDto, nameof(messageDto)).NotNull();

            try
            {
                uint peerDeletedCount = 0;

                var deserialised = messageDto.Payload.FromProtocolMessage<RemovePeerRequest>() ?? throw new ArgumentNullException(nameof(messageDto));
                var publicKeyIsEmpty = deserialised.PublicKey.IsEmpty;

                Guard.Argument(deserialised).NotNull();
                
                var peersToDelete = _peerRepository.GetAll().TakeWhile(peer =>
                    peer.PeerIdentifier.Ip.To16Bytes().SequenceEqual(deserialised.PeerIp.ToByteArray()) &&
                    (publicKeyIsEmpty || peer.PeerIdentifier.PublicKey.SequenceEqual(deserialised.PublicKey.ToByteArray()))).ToArray();
                
                if (peersToDelete.Length > 0)
                {
                    foreach (var peerToDelete in peersToDelete)
                    {
                        _peerRepository.Delete(peerToDelete);
                        peerDeletedCount += 1;
                    }
                }

                return new RemovePeerResponse
                {
                    DeletedCount = peerDeletedCount
                };
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
