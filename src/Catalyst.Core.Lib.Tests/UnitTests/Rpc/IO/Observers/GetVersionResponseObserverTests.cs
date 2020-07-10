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
using Catalyst.Core.Lib.IO.Messaging.Correlation;
using Catalyst.Core.Modules.Rpc.Client.IO.Observers;
using Catalyst.Protocol.Rpc.Node;
using Catalyst.TestUtils;
using NSubstitute;
using Serilog;
using NUnit.Framework;

namespace Catalyst.Core.Lib.Tests.UnitTests.Rpc.IO.Observers
{
    public sealed class GetVersionResponseObserverTests
    {
        [Test]
        public void Null_Version_Throws_Exception()
        {
            var senderAddress = MultiAddressHelper.GetAddress();
            var correlationId = CorrelationId.GenerateCorrelationId();

            var logger = Substitute.For<ILogger>();
            var getVersionResponseObserver = new GetVersionResponseObserver(logger);

            Assert.Throws<ArgumentNullException>(() => getVersionResponseObserver
               .HandleResponseObserver(new VersionResponse {Version = null}, senderAddress, correlationId));
        }
    }
}
