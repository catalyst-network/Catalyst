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

using System.Linq;
using Catalyst.Common.Config;
using Catalyst.Common.Enumerator;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.IO.Messaging.Handlers;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;

namespace Catalyst.Cli.Handlers
{
    /// <summary>
    /// Handles Get file from DFS response
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetFileFromDfsResponse, IMessageCorrelationCache}" />
    /// <seealso cref="IRpcResponseHandler" />
    public class GetFileFromDfsResponseHandler : CorrelatableMessageHandlerBase<GetFileFromDfsResponse, IMessageCorrelationCache>,
        IRpcResponseHandler
    {
        /// <summary>The file transfer</summary>
        private readonly IFileTransfer _fileTransfer;

        /// <summary>Initializes a new instance of the <see cref="GetFileFromDfsResponseHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileTransfer">The file transfer.</param>
        public GetFileFromDfsResponseHandler(IMessageCorrelationCache correlationCache,
            ILogger logger,
            IFileTransfer fileTransfer) : base(correlationCache, logger)
        {
            _fileTransfer = fileTransfer;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<GetFileFromDfsResponse>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            var responseCode = Enumeration.GetAll<FileTransferResponseCodes>().First(respCode => respCode.Id == deserialised.ResponseCode[0]);

            var fileTransferInformation = _fileTransfer.GetFileTransferInformation(message.Payload.CorrelationId.ToGuid());

            if (responseCode == FileTransferResponseCodes.Successful)
            {
                fileTransferInformation?.SetLength(deserialised.FileSize);
            }
            else
            {
                fileTransferInformation.Expire();
            }
        }
    }
}
