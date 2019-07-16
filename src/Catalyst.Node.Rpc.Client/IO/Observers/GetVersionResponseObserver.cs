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
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging.Correlation;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.IO.Messaging.Dto;
using Catalyst.Common.IO.Observers;
using Catalyst.Node.Rpc.Client.IO.Messaging.Dto;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetVersion request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class GetVersionResponseObserver
        : ResponseObserverBase<VersionResponse>,
            IRpcResponseObserver
    {
        private readonly IUserOutput _output;

        private readonly ReplaySubject<IRPCClientMessageDto<IMessage>> _messageResponse;
        public IObservable<IRPCClientMessageDto<IMessage>> MessageResponseStream { private set; get; }

        /// <summary>
        /// Handles the VersionResponse message sent from the <see>
        ///     <cref>GetVersionRequestHandler</cref>
        /// </see>
        /// .
        /// </summary>
        /// <param name="output">A service used to output the result of the messages handling to the user.</param>
        /// <param name="logger">Logger to log debug related information.</param>
        public GetVersionResponseObserver(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
            _messageResponse = new ReplaySubject<IRPCClientMessageDto<IMessage>>(1);
            MessageResponseStream = _messageResponse.AsObservable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="versionResponse"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(VersionResponse versionResponse,
            IChannelHandlerContext channelHandlerContext,
            IPeerIdentifier senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(versionResponse, nameof(versionResponse)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug("GetVersionResponseHandler starting ...");

            Guard.Argument(versionResponse, nameof(versionResponse)).NotNull("The message cannot be null");

            Guard.Argument(versionResponse, nameof(versionResponse)).NotNull("The VersionResponse cannot be null")
               .Require(d => d.Version != null,
                    d => $"{nameof(versionResponse)} must have a valid Version.");

            _messageResponse.OnNext(new RPCClientMessageDto<IMessage>(versionResponse, senderPeerIdentifier));
        }
    }
}
