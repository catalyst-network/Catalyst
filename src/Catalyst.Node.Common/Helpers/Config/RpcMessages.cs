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

using Catalyst.Node.Common.Helpers.Enumerator;
using Catalyst.Node.Common.Interfaces.IO.Messaging;

namespace Catalyst.Node.Common.Helpers.Config
{
    public class RpcMessages : Enumeration, IEnumerableMessageType
    {
        public static readonly RpcMessages GetInfoRequest = new GetInfoRequestMessage();
        public static readonly RpcMessages GetInfoResponse = new GetInfoResponseMessage();
        public static readonly RpcMessages GetMempoolRequest = new GetMempoolRequestMessage();
        public static readonly RpcMessages GetMempoolResponse = new GetMempoolResponseMessage();
        public static readonly RpcMessages GetVersionRequest = new GetVersionRequestMessage();
        public static readonly RpcMessages GetVersionResponse = new GetVersionResponseMessage();
        public static readonly RpcMessages SignMessageRequest = new SignMessageRequestMessage();
        public static readonly RpcMessages SignMessageResponse = new SignMessageResponseMessage();

        private RpcMessages(int id, string name) : base(id, name) { }

        private sealed class GetInfoRequestMessage : RpcMessages
        {
            public GetInfoRequestMessage() : base(1, "GetInfoRequest") { }
        }
        
        private sealed class GetInfoResponseMessage : RpcMessages
        {
            public GetInfoResponseMessage() : base(2, "GetInfoResponse") { }
        }
        
        private sealed class GetMempoolRequestMessage : RpcMessages
        {
            public GetMempoolRequestMessage() : base(3, "GetMempoolRequest") { }
        }
        
        private sealed class GetMempoolResponseMessage : RpcMessages
        {
            public GetMempoolResponseMessage() : base(4, "GetMempoolResponse") { }
        }
        
        private sealed class GetVersionRequestMessage : RpcMessages
        {
            public GetVersionRequestMessage() : base(5, "GetVersionRequest") { }
        }
        
        private sealed class GetVersionResponseMessage : RpcMessages
        {
            public GetVersionResponseMessage() : base(6, "GetVersionResponse") { }
        }
        
        private sealed class SignMessageRequestMessage : RpcMessages
        {
            public SignMessageRequestMessage() : base(7, "SignMessageRequest") { }
        }
        
        private sealed class SignMessageResponseMessage : RpcMessages
        {
            public SignMessageResponseMessage() : base(8, "SignMessageResponse") { }
        }
    }
}
