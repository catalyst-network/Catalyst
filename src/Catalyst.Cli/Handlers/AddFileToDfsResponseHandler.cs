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

using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.Rpc;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Add File to DFS Response handler
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{AddFileToDfsResponse, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcResponseHandler" />
    public sealed class AddFileToDfsResponseHandler : CorrelatableMessageHandlerBase<AddFileToDfsResponse, IMessageCorrelationCache>,
        IRpcResponseHandler
    {
        /// <summary>The cli file transfer</summary>
        private readonly IRpcFileTransfer _rpcFileTransfer;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsResponseHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcFileTransfer">The CLI file transfer</param>
        public AddFileToDfsResponseHandler(IMessageCorrelationCache correlationCache,
            ILogger logger,
            IRpcFileTransfer rpcFileTransfer) : base(correlationCache, logger)
        {
            _rpcFileTransfer = rpcFileTransfer;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<AddFileToDfsResponse>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            //@TODO check
            var responseCode = Enumeration.Parse<FileTransferResponseCodes>(deserialised.ResponseCode[0].ToString());

            if (responseCode == FileTransferResponseCodes.Failed || responseCode == FileTransferResponseCodes.Finished)
                _rpcFileTransfer.ProcessCompletedCallback(responseCode, deserialised.DfsHash);
            else
                _rpcFileTransfer.InitialiseFileTransferResponseCallback(responseCode);
        }
    }
}
