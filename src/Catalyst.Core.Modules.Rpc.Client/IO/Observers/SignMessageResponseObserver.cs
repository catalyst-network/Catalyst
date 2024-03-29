#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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
using Catalyst.Abstractions.Cli;
using Catalyst.Abstractions.IO.Messaging.Correlation;
using Catalyst.Core.Lib.Rpc.IO;
using Catalyst.Protocol.Peer;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using DotNetty.Transport.Channels;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Rpc.Client.IO.Observers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetMempool request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class SignMessageResponseObserver
        : RpcResponseObserver<SignMessageResponse>
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="logger">Logger to log debug related information.</param>
        public SignMessageResponseObserver(IUserOutput output,
            ILogger logger)
            : base(logger)
        {
            _output = output;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="signMessageRequest"></param>
        /// <param name="channelHandlerContext"></param>
        /// <param name="senderPeerIdentifier"></param>
        /// <param name="correlationId"></param>
        protected override void HandleResponse(SignMessageResponse signMessageRequest,
            IChannelHandlerContext channelHandlerContext,
            PeerId senderPeerIdentifier,
            ICorrelationId correlationId)
        {
            Guard.Argument(signMessageRequest, nameof(signMessageRequest)).NotNull();
            Guard.Argument(channelHandlerContext, nameof(channelHandlerContext)).NotNull();
            Guard.Argument(senderPeerIdentifier, nameof(senderPeerIdentifier)).NotNull();
            Logger.Debug(@"sign message response");
            
            try
            {
                var decodeResult = MultiBase.Decode(signMessageRequest.OriginalMessage.ToByteArray().ToString());

                Guard.Argument(decodeResult, nameof(decodeResult)).NotNull("The sign message response cannot be null.");

                var originalMessage = decodeResult.ToHexString();

                Guard.Argument(originalMessage, nameof(originalMessage)).NotNull();

                _output.WriteLine(
                    $@"Signature: {signMessageRequest.Signature.ToByteArray().ToBase32()} " +
                    $@"Public Key: {signMessageRequest.PublicKey.ToByteArray().ToBase32()} Original Message: {originalMessage}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle SignMessageResponseHandler after receiving message {0}", signMessageRequest);
                _output.WriteLine(ex.Message);
            }
            finally
            {
                Logger.Information("Press Enter to continue ...");
            }
        }
    }
}
