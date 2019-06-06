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

using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Protocol.Common;

namespace Catalyst.Common.Interfaces.P2P.Messaging
{
    /// <summary>
    /// Handler for Ask message where you want to manipulate reputation of the recipient depending if they respond/have a correlation.
    /// </summary>
    /// <typeparam name="TReputableCache">The type of the reputable cache.</typeparam>
    public interface IReputationAskHandler<out TReputableCache> where TReputableCache : IMessageCorrelationCache
    {
        /// <summary>Gets the reputable cache.</summary>
        /// <value>The reputable cache.</value>
        TReputableCache ReputableCache { get; }

        /// <summary>Determines whether this instance [can execute next handler] the specified message.</summary>
        /// <param name="message">The message.</param>
        /// <returns><c>true</c> if this instance [can execute next handler] the specified message; otherwise, <c>false</c>.</returns>
        bool CanExecuteNextHandler(IChanneledMessage<ProtocolMessage> message);
    }
}
