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
using System.Threading.Tasks;
using Catalyst.Common.Interfaces.IO;
using DotNetty.Transport.Channels;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Catalyst.Node.Core.UnitTests.IO
{
    public class NodeBusinessEventFactoryTests
    {
        private readonly INodeBusinessEventFactory _eventFactory;
        private const int ExpectedThreads = 2;
        private const int TaskDelay = 100;

        public NodeBusinessEventFactoryTests()
        {
            var configurationRoot = Substitute.For<IConfigurationRoot>();
            
            var fakeSection = Substitute.For<IConfigurationSection>();
            fakeSection.Value.Returns(ExpectedThreads.ToString());

            fakeSection.GetSection("Rpc").Returns(fakeSection);
            fakeSection.GetSection("Peer").Returns(fakeSection);
            fakeSection.GetSection("ServerBusinessThreads").Value.Returns(ExpectedThreads.ToString());
            fakeSection.GetSection("ClientBusinessThreads").Value.Returns(ExpectedThreads.ToString());

            configurationRoot.GetSection("CatalystNodeConfiguration").Returns(fakeSection);

            _eventFactory = new NodeBusinessEventFactory(configurationRoot);
        }
        
        [Fact]
        public void CreatesCorrectNumberOfRpcServerThreadsInEventLoopGroup()
        {
            CheckAmountOfThreadsCreated(_eventFactory.NewRpcServerLoopGroup());
        }

        [Fact]
        public void CreatesCorrectNumberOfUdpClientThreadsInEventLoopGroup()
        {
            CheckAmountOfThreadsCreated(_eventFactory.NewUdpClientLoopGroup());
        }

        [Fact]
        public void CreatesCorrectNumberOfUdpServerThreadsInEventLoopGroup()
        {
            CheckAmountOfThreadsCreated(_eventFactory.NewUdpServerLoopGroup());
        }

        private void CheckAmountOfThreadsCreated(IEventLoopGroup eventLoopGroup)
        {
            int threadCount = 0;

            for (int i = 0; i < ExpectedThreads * 2; i++)
            {
                eventLoopGroup.Execute(() =>
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
