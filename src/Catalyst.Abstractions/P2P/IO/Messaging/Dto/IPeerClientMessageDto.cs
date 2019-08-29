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

using Catalyst.Abstractions.IO.Messaging.Correlation;
using Google.Protobuf;

namespace Catalyst.Abstractions.P2P.IO.Messaging.Dto
{
    /// <summary>
    ///     Dto to be used to push IPPN protocol messages to an observable stream.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IPeerClientMessageDto
    {
        ICorrelationId CorrelationId { get; set; }
        IPeerIdentifier Sender { get; set; }
        IMessage Message { get; set; }
    }
}
