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

using Catalyst.Abstractions.P2P;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Core.P2P;
using Catalyst.Protocol.Common;

namespace Catalyst.Core.IO.Messaging.Dto
{
    public sealed class SignedMessageDto : BaseMessageDto<ProtocolMessageSigned>
    {
        /// <summary>
        ///     Data transfer object to wrap up all parameters for sending protocol messages into a MessageFactors.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="recipientPeerIdentifier"></param>
        public SignedMessageDto(ProtocolMessageSigned content,
            IPeerIdentifier recipientPeerIdentifier)
            : base(content, new PeerIdentifier(content.Message.PeerId), recipientPeerIdentifier,
                new CorrelationId(content.Message.CorrelationId)) { }
    }
}
