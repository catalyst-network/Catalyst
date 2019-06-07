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
using System.Collections.Generic;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.UnitTests.TestUtils;
using Catalyst.Node.Rpc.Client.Handlers;
using Catalyst.Protocol.Rpc.Node;
using DotNetty.Transport.Channels;
using Google.Protobuf;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Node.Rpc.Client.UnitTests.Handlers
{
    public sealed class GetFileFromDfsResponseHandlerTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _fileDownloadFactory;

        public GetFileFromDfsResponseHandlerTest()
        {
            var peerSettings = Substitute.For<IPeerSettings>();
            peerSettings.SeedServers.Returns(new List<string>
            {
                "catalyst.seedserver01.com",
                "catalyst.seedserver02.com"
            });

            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _fileDownloadFactory = new DownloadFileTransferFactory();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void CanExpireDownloadFileTransferOnError()
        {
            var fakeFileTransfer = Substitute.For<IDownloadFileInformation>();
            var guid = Guid.NewGuid();
            fakeFileTransfer.CorrelationGuid.Returns(guid);

            _fileDownloadFactory.RegisterTransfer(fakeFileTransfer);

            var getFileFromDfsResponseHandler =
                new GetFileFromDfsResponseHandler(_logger, _fileDownloadFactory);

            var getFileResponse = new GetFileFromDfsResponse
            {
                FileSize = 10,
                ResponseCode = ByteString.CopyFrom((byte) FileTransferResponseCodes.Error.Id)
            }.ToAnySigned(PeerIdHelper.GetPeerId("Test"), guid);
            getFileResponse.SendToHandler(_fakeContext, getFileFromDfsResponseHandler);

            fakeFileTransfer.Received(1).Expire();
        }
    }
}
