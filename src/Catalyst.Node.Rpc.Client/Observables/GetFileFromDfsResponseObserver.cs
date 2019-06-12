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
using System.IO;
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observables;
using Catalyst.Common.IO.Observables;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Rpc.Node;
using Dawn;
using Serilog;

namespace Catalyst.Node.Rpc.Client.Observables
{
    /// <summary>
    /// Handles Get file from DFS response
    /// </summary>
    /// <seealso cref="IRpcResponseObserver" />
    public sealed class GetFileFromDfsResponseObserver : 
        ResponseObserverBase<GetFileFromDfsResponse>,
        IRpcResponseObserver
    {
        /// <summary>The file transfer factory</summary>
        private readonly IDownloadFileTransferFactory _fileTransferFactory;

        /// <summary>Initializes a new instance of the <see cref="GetFileFromDfsResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="fileTransferFactory">The file transfer.</param>
        public GetFileFromDfsResponseObserver(ILogger logger,
            IDownloadFileTransferFactory fileTransferFactory) : base(logger)
        {
            _fileTransferFactory = fileTransferFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto, nameof(messageDto)).NotNull("Message cannot be null");

            var deserialised = messageDto.Payload.FromProtocolMessage<GetFileFromDfsResponse>() ?? throw new ArgumentNullException(nameof(messageDto));
            
            // @TODO return int not byte
            // var responseCode = Enumeration.Parse<FileTransferResponseCodes>(deserialised.ResponseCode[0].ToString());

            var responseCode = (FileTransferResponseCodes) deserialised.ResponseCode[0];

            if (_fileTransferFactory != null) 
            {
                var fileTransferInformation = _fileTransferFactory.GetFileTransferInformation(messageDto.Payload.CorrelationId.ToGuid());

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
                    }).ConfigureAwait(false);
                }
                else
                {
                    fileTransferInformation.Expire();
                }
            }
        }
    }
}
