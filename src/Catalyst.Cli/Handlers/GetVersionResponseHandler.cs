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
using Catalyst.Common.Extensions;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;
using Dawn;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetVersion request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class GetVersionResponseHandler
        : CorrelatableMessageHandlerBase<VersionResponse, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// Handles the VersionResponse message sent from the <see cref="GetVersionRequestHandler" />.
        /// </summary>
        /// <param name="output">A service used to output the result of the messages handling to the user.</param>
        /// <param name="messageCorrelationCache"></param>
        /// <param name="logger">Logger to log debug related information.</param>
        public GetVersionResponseHandler(IUserOutput output,
            IMessageCorrelationCache messageCorrelationCache,
            ILogger logger)
            : base(messageCorrelationCache, logger)
        {
            _output = output;
        }

        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            Guard.Argument(message).NotNull("The message cannot be null");
            
            Logger.Debug("GetVersionResponseHandler starting ...");
            
            try
            {
                var deserialised = message.Payload.FromAnySigned<VersionResponse>();
                
                Guard.Argument(deserialised, nameof(deserialised)).NotNull("The VersionResponse cannot be null")
                   .Require(d => d.Version != null,
                        d => $"{nameof(deserialised)} must have a valid Version.");
                
                _output.WriteLine($"Node Version: {deserialised.Version}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetInfoResponse after receiving message {0}", message);
                _output.WriteLine(ex.Message);
            }
            finally
            {
                Logger.Information("Press Enter to continue ...");
            }
        }
    }
}
