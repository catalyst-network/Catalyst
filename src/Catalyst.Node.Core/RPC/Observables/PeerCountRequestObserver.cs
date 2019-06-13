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
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Messaging;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Common.IO.Observables;
using Catalyst.Common.P2P;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Google.Protobuf;
using Serilog;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.RPC.Observables
{
    /// <summary>
    /// Peer count request handler
    /// </summary>
    /// <seealso cref="IRpcRequestObserver" />
    public sealed class PeerCountRequestObserver
        : RequestObserverBase<GetPeerCountRequest>,
            IRpcRequestObserver
    {
        /// <summary>The peer discovery</summary>
        private readonly IRepository<Peer> _peerRepository;

        /// <summary>Initializes a new instance of the <see cref="PeerCountRequestObserver"/> class.</summary>
        /// <param name="peerIdentifier">The peer identifier.</param>
        /// <param name="peerRepository">The peer discovery.</param>
        /// <param name="logger">The logger.</param>
        public PeerCountRequestObserver(IPeerIdentifier peerIdentifier,
            IRepository<Peer> peerRepository,
            ILogger logger) :
            base(logger, peerIdentifier)
        {
            _peerRepository = peerRepository;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override IMessage HandleRequest(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto, nameof(messageDto)).NotNull("Received message cannot be null");

            var peerCount = _peerRepository.GetAll().Count();

            return new GetPeerCountResponse
            {
                PeerCount = peerCount
            };
        }
    }
}
