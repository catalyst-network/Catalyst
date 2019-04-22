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
using Catalyst.Node.Common.Helpers.Extensions;
using Catalyst.Node.Common.Helpers.IO.Messaging.Handlers;
using Catalyst.Node.Common.Interfaces.IO.Inbound;
using Catalyst.Node.Common.Interfaces.IO.Messaging;
using Catalyst.Node.Common.Interfaces.Cli;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Multiformats.Base;
using Nethereum.RLP;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetMempool request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class SignMessageResponseHandler
        : CorrelatableMessageHandlerBase<SignMessageResponse, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="messageCorrelationCache"></param>
        /// <param name="logger">Logger to log debug related information.</param>
        public SignMessageResponseHandler(IUserOutput output,
            IMessageCorrelationCache messageCorrelationCache,
            ILogger logger)
            : base(messageCorrelationCache, logger)
        {
            _output = output;
        }

        /// <summary>
        /// Handles the VersionResponse message sent from the <see />.
        /// </summary>
        /// <param name="message">An object of GetMempoolResponse</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            try
            {
                var deserialised = message.Payload.FromAnySigned<SignMessageResponse>();

                var decodeResult = RLP.Decode(deserialised.OriginalMessage.ToByteArray())[0].RLPData;

                Guard.Argument(decodeResult).NotNull();

                var originalMessage = decodeResult.ToStringFromRLPDecoded();

                _output.WriteLine(
                    $"Signature: {Multibase.Encode(MultibaseEncoding.Base64, deserialised.Signature.ToByteArray())}\n" +
                    $"Public Key: {Multibase.Encode(MultibaseEncoding.Base58Btc, deserialised.PublicKey.ToByteArray())}\nOriginal Message: {originalMessage}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle SignMessageResponseHandler after receiving message {0}", message);
                _output.WriteLine(ex.Message);
            }
            finally
            {
                Logger.Information("Press Enter to continue ...");
            }
        }
    }
}
