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
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Catalyst.Abstractions.FileTransfer;
using Catalyst.Core.IO.Messaging.Correlation;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Cli.IntegrationTests.Commands
{
    public sealed class GetFileCommandTests : CliCommandTestsBase
    {
        public static IEnumerable<object[]> GetFileData =>
            new List<object[]>
            {
                new object[] {"/fake_file_hash", AppDomain.CurrentDomain.BaseDirectory + "/Config/addfile_test.json", true}
            };

        public GetFileCommandTests(ITestOutputHelper output) : base(output) { }

        [Theory]
        [MemberData(nameof(GetFileData))]
        public async Task Cli_Can_Send_Get_File_Request(string fileHash, string outputPath, bool expectedResult)
        {
            var downloadFileFactory = Scope.Resolve<IDownloadFileTransferFactory>();

            var task = Task.Run(() =>
                Shell.ParseCommand("getfile", NodeArgumentPrefix, ServerNodeName, "-f", fileHash, "-o", outputPath)
            );

            await TaskHelper.WaitForAsync(() => downloadFileFactory.Keys.Length > 0, TimeSpan.FromSeconds(5));

            downloadFileFactory.GetFileTransferInformation(new CorrelationId(downloadFileFactory.Keys.First())).Expire();

            var result = await task.ConfigureAwait(false);
            result.Should().Be(expectedResult);

            if (expectedResult)
            {
                AssertSentMessageAndGetMessageContent<GetFileFromDfsRequest>();
            }
        }
    }
}
