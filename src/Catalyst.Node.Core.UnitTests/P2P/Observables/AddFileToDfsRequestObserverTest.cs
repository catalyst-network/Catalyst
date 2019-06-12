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
using Catalyst.Common.FileTransfer;
using Catalyst.Common.Interfaces.FileTransfer;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.P2P;
using Catalyst.Node.Core.Modules.Dfs;
using Catalyst.Node.Core.RPC.Observables;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using Ipfs.CoreApi;
using NSubstitute;
using Serilog;
using System;
using Ipfs.CoreApi;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.P2P.Observables
{
    public sealed class AddFileToDfsRequestObserverTest
    {
        private readonly ILogger _logger;
        private readonly IChannelHandlerContext _fakeContext;
        private readonly IDownloadFileTransferFactory _nodeFileTransferFactory;
        private readonly IMessageFactory _messageFactory;
        private ICoreApi _ipfsEngine;

        public AddFileToDfsRequestObserverTest()
        {
            _logger = Substitute.For<ILogger>();
            _fakeContext = Substitute.For<IChannelHandlerContext>();
            _messageFactory = Substitute.For<IMessageFactory>();
            _nodeFileTransferFactory = new DownloadFileTransferFactory();
            _logger = Substitute.For<ILogger>();
        }

        [Fact]
        public void HandlerCanInitializeDownloadFileTransfer()
        {
            var sender = PeerIdHelper.GetPeerId("sender");
            var handler = new AddFileToDfsRequestObserver(new Dfs(_ipfsEngine, _logger), new PeerIdentifier(sender),
                _nodeFileTransferFactory, _messageFactory, _logger);

            //Create a response object and set its return value
            var request = new AddFileToDfsRequest
            {
                Node = "node1",
                FileName = "Test.dat",
                FileSize = 10000
            }.ToProtocolMessage(sender, Guid.NewGuid());
            request.SendToHandler(_fakeContext, handler);

            Assert.Equal(1, _nodeFileTransferFactory.Keys.Length);
        }
    }
}
