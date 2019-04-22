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
using Catalyst.Common.Interfaces.IO.Messaging;

namespace Catalyst.Common.Config
{
    public class P2PMessages
        : Enumeration,
            IEnumerableMessageType
    {
        public static readonly P2PMessages PingRequest = new PingRequestMessage();
        public static readonly P2PMessages PingResponse = new PingResponseMessage();
        public static readonly P2PMessages GetNeighbourRequest = new GetNeighbourRequestMessage();
        public static readonly P2PMessages GetNeighbourResponse = new GetNeighbourResponseMessage();
        public static readonly P2PMessages BroadcastTransaction = new BroadcastTransactionMessage();

        private P2PMessages(int id, string name) : base(id, name) { }

        private sealed class PingRequestMessage : P2PMessages
        {
            public PingRequestMessage() : base(1, "PingRequest") { }
        }

        private sealed class PingResponseMessage : P2PMessages
        {
            public PingResponseMessage() : base(2, "PingResponse") { }
        }
        
        private sealed class GetNeighbourRequestMessage : P2PMessages
        {
            public GetNeighbourRequestMessage() : base(3, "GetNeighbourRequest") { }
        }

        private sealed class GetNeighbourResponseMessage : P2PMessages
        {
            public GetNeighbourResponseMessage() : base(4, "GetNeighbourResponse") { }
        }
        
        private sealed class BroadcastTransactionMessage : P2PMessages
        {
            public BroadcastTransactionMessage() : base(5, "BroadcastTransaction") { }
        }
    }
}
