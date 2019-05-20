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

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    /// Handles Get file from DFS response
    /// </summary>
    /// <seealso cref="CorrelatableMessageHandlerBase{GetFileFromDfsResponse, IRpcCorrelationCache}" />
    /// <seealso cref="IRpcResponseHandler" />
    public class GetFileFromDfsResponseHandler : CorrelatableMessageHandlerBase<GetFileFromDfsResponse, IRpcCorrelationCache>,
        IRpcResponseHandler
    {
        /// <summary>The file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="GetFileFromDfsResponseHandler"/> class.</summary>
        /// <param name="correlationCache">The correlation cache.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileTransferFactory">The file transfer.</param>
        public GetFileFromDfsResponseHandler(IRpcCorrelationCache correlationCache,
            ILogger logger,
            IDownloadFileTransferFactory fileTransferFactory) : base(correlationCache, logger)
        {
            _fileTransferFactory = fileTransferFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="message">The message.</param>
        protected override void Handler(IChanneledMessage<AnySigned> message)
        {
            var deserialised = message.Payload.FromAnySigned<GetFileFromDfsResponse>();

            Guard.Argument(deserialised).NotNull("Message cannot be null");

            var responseCode = (FileTransferResponseCodes) deserialised.ResponseCode[0];

            var fileTransferInformation = _fileTransferFactory.GetFileTransferInformation(message.Payload.CorrelationId.ToGuid());

            if (fileTransferInformation == null)
            {
                return;
            }

            if (responseCode == FileTransferResponseCodes.Successful)
            {
                fileTransferInformation.SetLength(deserialised.FileSize);

                _fileTransferFactory.FileTransferAsync(fileTransferInformation.CorrelationGuid, CancellationToken.None).ContinueWith(task =>
                {
                    File.Move(fileTransferInformation.TempPath, fileTransferInformation.FileOutputPath);
                });
            }
            else
            {
                fileTransferInformation.Expire();
            }
        }
    }
}
