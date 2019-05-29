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

using Catalyst.Cli.Rpc;
using Catalyst.Common.Interfaces.IO;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace Catalyst.Cli.UnitTests
{
    public class RpcBusinessEventFactoryTest
    {
        private readonly IRpcBusinessEventFactory _eventFactory;
        private const int ExpectedThreads = 2;

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
        public void CanCreateNewRpcClientLoopGroup()
        {
            Assert.NotNull(_eventFactory.NewRpcClientLoopGroup());
        }
    }
}
