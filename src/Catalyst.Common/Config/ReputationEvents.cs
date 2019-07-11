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

using Catalyst.Common.Enumerator;
using Catalyst.Common.Interfaces.Config;

namespace Catalyst.Common.Config
{
    public class ReputationEvents : Enumeration, IReputationEvents
    {
        public static readonly ReputationEvents NoResponseReceived = new NoResponseReceivedEvent();
        public static readonly ReputationEvents ResponseReceived = new ResponseReceivedEvent();
        public static readonly ReputationEvents UnCorrelatableMessage = new UnCorrelatableMessageEvent();
        public static readonly ReputationEvents InvalidMessageSignature = new InvalidMessageSignatureEvent();
        
        public int Amount { get; set; }

        private ReputationEvents(int id, string name) : base(id, name) { }
        
        /// <summary>
        ///     Fires when a message is evicted for the PeerMessageCorrelationManager cache.
        ///     This means a node has failed to respond to a message.
        /// </summary>
        private sealed class NoResponseReceivedEvent : ReputationEvents
        {
            public NoResponseReceivedEvent() : base(1, "noResponseReceived") { Amount = -10; }
        }
        
        /// <summary>
        ///     Fires when a message is matched PeerMessageCorrelationManager cache.
        ///     This means a node has correctly respond to a message.
        /// </summary>
        private sealed class ResponseReceivedEvent : ReputationEvents
        {
            public ResponseReceivedEvent() : base(2, "responseReceived") { Amount = 10; }
        }
        
        /// <summary>
        ///     Fires when a message is received but can't be correlated by the PeerMessageCorrelationManager cache.
        ///     This means a node has either sent a message with an invalid CorrelationId, it's responded too late,
        ///     or sent a message to the wrong node.
        /// </summary>
        private sealed class UnCorrelatableMessageEvent : ReputationEvents
        {
            public UnCorrelatableMessageEvent() : base(3, "unCorrelatableMessage") { Amount = -100; }
        }
        
        /// <summary>
        ///     Fired when node receives a message with an invalid signature.
        /// </summary>
        private sealed class InvalidMessageSignatureEvent : ReputationEvents
        {
            public InvalidMessageSignatureEvent() : base(4, "invalidMessageSignature") { Amount = -1000; }
        }
    }
}
