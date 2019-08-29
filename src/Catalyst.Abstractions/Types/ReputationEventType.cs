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

using Catalyst.Abstractions.Config;
using Catalyst.Abstractions.Enumerator;

namespace Catalyst.Abstractions.Types
{
    public class ReputationEventType : Enumeration, IReputationEvents
    {
        public static readonly ReputationEventType NoResponseReceived = new NoResponseReceivedEventType();
        public static readonly ReputationEventType ResponseReceived = new ResponseReceivedEventType();
        public static readonly ReputationEventType UnCorrelatableMessage = new UnCorrelatableMessageEventType();
        public static readonly ReputationEventType InvalidMessageSignature = new InvalidMessageSignatureEventType();
        
        public int Amount { get; set; }

        private ReputationEventType(int id, string name) : base(id, name) { }
        
        /// <summary>
        ///     Fires when a message is evicted for the PeerMessageCorrelationManager cache.
        ///     This means a node has failed to respond to a message.
        /// </summary>
        private sealed class NoResponseReceivedEventType : ReputationEventType
        {
            public NoResponseReceivedEventType() : base(1, "noResponseReceived") { Amount = -10; }
        }
        
        /// <summary>
        ///     Fires when a message is matched PeerMessageCorrelationManager cache.
        ///     This means a node has correctly respond to a message.
        /// </summary>
        private sealed class ResponseReceivedEventType : ReputationEventType
        {
            public ResponseReceivedEventType() : base(2, "responseReceived") { Amount = 10; }
        }
        
        /// <summary>
        ///     Fires when a message is received but can't be correlated by the PeerMessageCorrelationManager cache.
        ///     This means a node has either sent a message with an invalid CorrelationId, it's responded too late,
        ///     or sent a message to the wrong node.
        /// </summary>
        private sealed class UnCorrelatableMessageEventType : ReputationEventType
        {
            public UnCorrelatableMessageEventType() : base(3, "unCorrelatableMessage") { Amount = -100; }
        }
        
        /// <summary>
        ///     Fired when node receives a message with an invalid signature.
        /// </summary>
        private sealed class InvalidMessageSignatureEventType : ReputationEventType
        {
            public InvalidMessageSignatureEventType() : base(4, "invalidMessageSignature") { Amount = -1000; }
        }
    }
}
