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

using Catalyst.Common.Interfaces.Util;
using Catalyst.Common.Rpc.IO.Messaging.Correlation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Serilog;
using Xunit;

namespace Catalyst.Common.UnitTests.IO.Messaging.Correlation
{
    public class RpcMessageCorrelationManagerTests
    {
        protected RpcMessageCorrelationManagerTests()
        {
            var testScheduler = new TestScheduler();
            var memoryCache = Substitute.For<IMemoryCache>();
            var logger = Substitute.For<ILogger>();
            var changeTokenProvider = Substitute.For<IChangeTokenProvider>();

            _rpcMessageCorrelationManager =
                new RpcMessageCorrelationManager(memoryCache, logger, changeTokenProvider, testScheduler);
        }

        private readonly RpcMessageCorrelationManager _rpcMessageCorrelationManager;

        [Fact]
        public void Dispose_Should_Dispose_RpcMessageCorrelationManager() { _rpcMessageCorrelationManager.Dispose(); }
    }
}
