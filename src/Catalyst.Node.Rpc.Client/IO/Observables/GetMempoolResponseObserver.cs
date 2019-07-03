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

using System;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.IO.Observables
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetMempool request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class GetMempoolResponseObserver
        : ResponseObserverBase<GetMempoolResponse>,
            IRpcResponseObserver
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// <param name="output">
        ///     A service used to output the result of the messages handling to the user.
        /// </param>
        /// <param name="logger">
        ///     Logger to log debug related information.
        /// </param>
        /// </summary>
        public GetMempoolResponseObserver(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="getMempoolResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(GetMempoolResponse getMempoolResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(getMempoolResponse, nameof(getMempoolResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("GetMempoolResponseHandler starting ...");

            Guard.Argument(getMempoolResponse, nameof(getMempoolResponse)).NotNull("The GetMempoolResponse cannot be null")
               .Require(d => d.Mempool != null,
                    d => $"{nameof(getMempoolResponse)} must have a valid Mempool.");
            
            try
            {
                for (var i = 0; i < getMempoolResponse.Mempool.Count; i++)
                {
                    _output.WriteLine($"tx{i}: {getMempoolResponse.Mempool[i]},");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetMempoolResponse after receiving message {0}", getMempoolResponse);
                _output.WriteLine(ex.Message);
            }
        }
    }
}
