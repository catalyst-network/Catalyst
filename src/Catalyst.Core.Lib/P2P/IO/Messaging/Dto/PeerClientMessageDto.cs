#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Abstractions.P2P.IO.Messaging.Dto;
using Dawn;
using Google.Protobuf;
using MultiFormats;

namespace Catalyst.Core.Lib.P2P.IO.Messaging.Dto
{
    public sealed class PeerClientMessageDto : IPeerClientMessageDto
    {
        public ICorrelationId CorrelationId { get; set; }
        public MultiAddress Sender { get; set; }
        public IMessage Message { get; set; }

        public PeerClientMessageDto(IMessage message, MultiAddress sender, ICorrelationId correlationId)
        {
            var ns = message.GetType().Namespace;
            Guard.Argument(message, nameof(message))
               .Require(ns != null && ns.Contains("IPPN"));
            Message = message;
            Sender = sender;
            CorrelationId = correlationId;
        }
    }
}
