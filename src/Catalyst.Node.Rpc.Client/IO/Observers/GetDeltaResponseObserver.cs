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

using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Serilog;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <inheritdoc cref="IRpcResponseObserver"/>
    /// <inheritdoc cref="ResponseObserverBase{TProto}"/>
    public class GetDeltaResponseObserver : RpcResponseObserver<GetDeltaResponse>
    {
        public static readonly string UnableToRetrieveDeltaMessage = "Unable to retrieve delta.";
        private readonly IUserOutput _output;

        public GetDeltaResponseObserver(IUserOutput output, ILogger logger) : base(logger)
        {
            _output = output;
        }

        /// <inheritdoc />
        protected override void HandleResponse(GetDeltaResponse deltaResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            if (deltaResponse.Delta == null)
            {
                _output.WriteLine(UnableToRetrieveDeltaMessage);
            }

            _output.WriteLine(deltaResponse.Delta.ToJsonString());
        }
    }
}
