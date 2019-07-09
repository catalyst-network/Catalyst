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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Extensions;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.IO.Messaging.Correlation;
using Catalyst.Common.IO.Messaging.Dto;
using Catalyst.Node.Core.P2P.IO.Observers;
using Catalyst.Protocol.IPPN;
using Catalyst.TestUtils;
using DotNetty.Transport.Channels;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Serilog;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Node.Core.IntegrationTests.P2P.IO.Observers
{
    public sealed class GetNeighbourResponseObserverTests : ConfigFileBasedTest
    {
        public GetNeighbourResponseObserverTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Can_Resolve_GetNeighbourResponseObserver_From_Container()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Dev)))
               .Build();
            
            ConfigureContainerBuilder(config, true, true);

            var container = ContainerBuilder.Build();
            using (container.BeginLifetimeScope(CurrentTestName))
            {
                var p2PMessageHandlers = container.Resolve<IEnumerable<IP2PMessageObserver>>();
                IEnumerable<IP2PMessageObserver> getNeighbourResponseHandler = p2PMessageHandlers.OfType<GetNeighbourResponseObserver>();
                getNeighbourResponseHandler.First().Should().BeOfType(typeof(GetNeighbourResponseObserver));
            }
        }
    }
}
