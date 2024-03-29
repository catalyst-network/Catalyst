#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Wire;

namespace Catalyst.Core.Lib.IO.Messaging.Dto
{
    public sealed class SignedMessageDto : BaseMessageDto<ProtocolMessage>
    {
        /// <summary>
        ///     Data transfer object to wrap up all parameters for sending protocol messages into a MessageFactors.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="recipientPeerIdentifier"></param>
        public SignedMessageDto(ProtocolMessage content,
            PeerId recipientPeerIdentifier)
            : base(content, content.PeerId, recipientPeerIdentifier,
                new CorrelationId(content.CorrelationId)) { }
    }
}
