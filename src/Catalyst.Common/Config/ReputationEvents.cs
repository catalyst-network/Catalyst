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

namespace Catalyst.Common.Config
{
    public interface IReputationEvents {
        int Amount { get; set; }
        string Name { get; }
        int Id { get; }
        bool Equals(Enumeration other);
        bool Equals(object obj);
    }

    public class ReputationEvents : Enumeration, IReputationEvents
    {
        public static readonly ReputationEvents NoResponseReceived = new NoResponseReceivedEvent();
        public static readonly ReputationEvents InvalidMessageSignature = new InvalidMessageSignatureEvent();
        
        public int Amount { get; set; }
        
        protected ReputationEvents(int id, string name) : base(id, name) { }
        
        /// <summary>
        ///     Fires when a message is evicted for the PeerMessageCorrelationManager cache.
        ///     This means a node has failed to respond to a message.
        /// </summary>
        private sealed class NoResponseReceivedEvent : ReputationEvents
        {
            public NoResponseReceivedEvent() : base(1, "noResponseReceived") { Amount = 10; }
        }
        
        /// <summary>
        ///     Fired when node receives an invalid message.
        /// </summary>
        private sealed class InvalidMessageSignatureEvent : ReputationEvents
        {
            public InvalidMessageSignatureEvent() : base(1, "invalidMessageSignature") { Amount = 100; }
        }
    }
}
