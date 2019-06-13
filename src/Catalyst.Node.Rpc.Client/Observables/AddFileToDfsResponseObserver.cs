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
using System.Threading;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.Cli;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;
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
    /// Add File to DFS Response handler
    /// </summary>
    /// <seealso cref="IRpcResponseMessageObserver" />
    public sealed class AddFileToDfsResponseObserver : 
        ResponseObserverBase<AddFileToDfsResponse>,
        IRpcResponseMessageObserver
    {
        /// <summary>The upload file transfer factory</summary>
        private readonly IUploadFileTransferFactory _rpcFileTransferFactory;

        private readonly IUserOutput _userOutput;

        /// <summary>Initializes a new instance of the <see cref="AddFileToDfsResponseObserver"/> class.</summary>
        /// <param name="logger">The logger.</param>
        /// <param name="rpcFileTransferFactory">The upload file transfer factory</param>
        /// <param name="userOutput"></param>
        public AddFileToDfsResponseObserver(ILogger logger,
            IUploadFileTransferFactory rpcFileTransferFactory, 
            IUserOutput userOutput) : base(logger)
        {
            _userOutput = userOutput;
            _rpcFileTransferFactory = rpcFileTransferFactory;
        }

        /// <summary>Handles the specified message.</summary>
        /// <param name="messageDto">The message.</param>
        public override void HandleResponse(IProtocolMessageDto<ProtocolMessage> messageDto)
        {
            Guard.Argument(messageDto, nameof(messageDto)).NotNull("Message cannot be null");

            var deserialised = messageDto.Payload.FromProtocolMessage<AddFileToDfsResponse>() ?? throw new ArgumentNullException(nameof(messageDto));
            
            // @TODO return int not byte
            // var responseCode = Enumeration.Parse<FileTransferResponseCodes>(deserialised.ResponseCode[0].ToString());

            var responseCode = (FileTransferResponseCodes) deserialised.ResponseCode[0];

            if (responseCode == FileTransferResponseCodes.Failed || responseCode == FileTransferResponseCodes.Finished)
            {
                _userOutput.WriteLine("File transfer completed, Response: " + responseCode.Name + " Dfs Hash: " + deserialised.DfsHash);
            }
            else
            {
                if (responseCode == FileTransferResponseCodes.Successful)
                {
                    _rpcFileTransferFactory.FileTransferAsync(messageDto.Payload.CorrelationId.ToGuid(), CancellationToken.None)
                       .ConfigureAwait(false);
                }
                else
                {
                    _rpcFileTransferFactory.Remove(messageDto.Payload.CorrelationId.ToGuid());
                }
            }
        }
    }
}
