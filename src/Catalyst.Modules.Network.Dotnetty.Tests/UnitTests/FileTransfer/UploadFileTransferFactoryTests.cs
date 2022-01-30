#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Threading;
using System.Threading.Tasks;
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using NSubstitute;
using Serilog;
using NUnit.Framework;
using Catalyst.Modules.Network.Dotnetty.Abstractions.FileTransfer;
using Catalyst.Modules.Network.Dotnetty.FileTransfer;

namespace Catalyst.Modules.Network.Dotnetty.Tests.UnitTests.FileTransfer
{
    public class UploadFileTransferFactoryTests
    {
        private IUploadFileTransferFactory _uploadFileTransferFactory;

        [SetUp]
        public void Init()
        {
            _uploadFileTransferFactory = new UploadFileTransferFactory(Substitute.For<ILogger>());
        }

        [TestCase(2u)]
        [TestCase(3u)]
        public async Task Can_Upload_File(uint numberOfChunks)
        {
            var uploadFileInformation = Substitute.For<IUploadFileInformation>();
            var correlationId = CorrelationId.GenerateCorrelationId();

            uploadFileInformation.MaxChunk.Returns(numberOfChunks);
            uploadFileInformation.CorrelationId.Returns(correlationId);

            _uploadFileTransferFactory.RegisterTransfer(uploadFileInformation);

            await _uploadFileTransferFactory.FileTransferAsync(correlationId, CancellationToken.None).ConfigureAwait(false);

            for (uint i = 0; i < numberOfChunks; i++)
            {
                uploadFileInformation.Received(1).GetUploadMessageDto(i);
                uploadFileInformation.Received(1).UpdateChunkIndicator(i, true);
            }

            await uploadFileInformation.RecipientChannel.ReceivedWithAnyArgs((int) numberOfChunks).WriteAndFlushAsync(default);
        }
    }
}
