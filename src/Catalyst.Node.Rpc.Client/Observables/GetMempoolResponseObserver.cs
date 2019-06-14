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
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using ILogger = Serilog.ILogger;

namespace Catalyst.Node.Rpc.Client.Observables
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
        /// Handles the VersionResponse message sent from the <see cref="GetMempoolRequestHandler" />.
        /// </summary>
        /// <param name="messageDto">An object of GetMempoolResponse</param>
        public override void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Logger.Debug("GetMempoolResponseHandler starting ...");

            Guard.Argument(messageDto, nameof(messageDto)).NotNull("The message cannot be null");
            
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<GetMempoolResponse>() ?? throw new ArgumentNullException(nameof(messageDto));
                
                Guard.Argument(deserialised, nameof(deserialised)).NotNull("The GetMempoolResponse cannot be null")
                   .Require(d => d.Mempool != null,
                        d => $"{nameof(deserialised)} must have a valid Mempool.");
                
                for (var i = 0; i < deserialised.Mempool.Count; i++)
                {
                    _output.WriteLine($"tx{i}: {deserialised.Mempool[i]},");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex,
                    "Failed to handle GetMempoolResponse after receiving message {0}", messageDto);
                _output.WriteLine(ex.Message);
            }
        }
    }
}
