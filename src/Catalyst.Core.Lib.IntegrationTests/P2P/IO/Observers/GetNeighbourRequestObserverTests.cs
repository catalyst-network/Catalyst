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
using Autofac;
using Catalyst.Common.Config;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Core.Lib.P2P.IO.Observers;
using Catalyst.TestUtils;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.IntegrationTests.P2P.IO.Observers
{
    public sealed class GetNeighbourRequestObserverTests : ConfigFileBasedTest
    {
        public GetNeighbourRequestObserverTests(ITestOutputHelper output) : base(output) { }

        [Fact(Skip = "un-needed")]
        public void Can_Resolve_GetNeighbourRequestObserver_From_Container()
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.ComponentsJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.SerilogJsonConfigFile))
               .AddJsonFile(Path.Combine(Constants.ConfigSubFolder, Constants.NetworkConfigFile(Network.Test)))
               .Build();

            ConfigureContainerBuilder(config, true, true);

            using (var container = ContainerBuilder.Build())
            {
                using (container.BeginLifetimeScope(CurrentTestName))
                {
                    var p2PMessageHandlers = container.Resolve<IEnumerable<IP2PMessageObserver>>();
                    IEnumerable<IP2PMessageObserver> getNeighbourResponseHandler =
                        p2PMessageHandlers.OfType<GetNeighbourRequestObserver>();
                    getNeighbourResponseHandler.First().Should().BeOfType(typeof(GetNeighbourRequestObserver));
                }
            }
        }
    }
}
