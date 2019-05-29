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

using System.Threading;
using System.Threading.Tasks;
using Catalyst.Cli.Rpc;
using Catalyst.Common.Interfaces.IO;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public class RpcBusinessEventFactoryTest
    {
        private readonly IRpcBusinessEventFactory _eventFactory;
        private const int ExpectedThreads = 2;
        private const int TaskDelay = 100;

        public RpcBusinessEventFactoryTest()
        {
            var fakeSection = Substitute.For<IConfigurationSection>();
            var configurationRoot = Substitute.For<IConfigurationRoot>();
            fakeSection.Value.Returns(ExpectedThreads.ToString());
            fakeSection.GetSection("ClientBusinessThreads").Returns(fakeSection);
            configurationRoot.GetSection("CatalystCliRpcNodes").Returns(fakeSection);
            _eventFactory = new RpcBusinessEventFactory(configurationRoot);
        }

        [Fact]
        public void CorrectNumberOfRpcThreadsCreatedInEventLoop()
        {
            int threadCount = 0;
            IEventLoopGroup eventLoop = _eventFactory.NewRpcClientLoopGroup();

            for (int i = 0; i < ExpectedThreads * 2; i++)
            {
                eventLoop.Execute(() =>
                {
                    threadCount = 0;
                    Task.Delay(TaskDelay).ConfigureAwait(false).GetAwaiter().GetResult();
                    Interlocked.Add(ref threadCount, 1);
                });
            }

            Task.Delay(TaskDelay * ExpectedThreads * 2 + 100).ConfigureAwait(false).GetAwaiter().GetResult();
            Assert.Equal(ExpectedThreads, threadCount);
        }
    }
}
