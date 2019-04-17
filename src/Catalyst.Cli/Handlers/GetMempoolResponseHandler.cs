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
using Catalyst.Node.Common.Interfaces.P2P.Messaging;
using Catalyst.Node.Common.Interfaces;
using Catalyst.Node.Core.RPC.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using ILogger = Serilog.ILogger;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handler responsible for handling the server's response for the GetMempool request.
    /// The handler reads the response's payload and formats it in user readable format and writes it to the console.
    /// </summary>
    public sealed class GetMempoolResponseHandler
        : AbstractCorrelatableAbstractMessageHandler<GetMempoolResponse, IMessageCorrelationCache>,
            IRpcResponseHandler
    {
        private readonly IUserOutput _output;

        /// <summary>
        /// Constructor. Calls the base class <see cref="AbstractCorrelatableAbstractMessageHandler{TProto,TCorrelator}"/> constructor.
        /// <param name="output">A service used to output the result of the messages handling to the user.</param>
        /// <param name="logger">Logger to log debug related information.</param>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="messageCorrelationCache"></param>
        /// <param name="logger">Logger to log debug related information.</param>
        public GetMempoolResponseHandler(IUserOutput output,
            IMessageCorrelationCache messageCorrelationCache,
            ILogger logger)
            : base(messageCorrelationCache, logger)
        {
            _output = output;
        }

        /// <summary>
        /// Handles the VersionResponse message sent from the <see cref="GetMempoolRequestHandler" />.
        /// </summary>
        /// <param name="message">An object of GetMempoolResponse</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            try
            {
                var deserialised = message.Payload.FromAnySigned<GetMempoolResponse>();

                foreach (var encodedTx in deserialised.Info.Values)
                {
                    _output.WriteLine($"t{deserialised.Info.Values.GetEnumerator().Current}: {encodedTx},");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetMempoolResponse after receiving message {0}", message);
                _output.WriteLine(ex.Message);
            }
            finally
            {
                Logger.Information("Press Enter to continue ...");
            }
        }
    }
}
