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
using Catalyst.Core.Util;
using FluentAssertions;
using Xunit;

namespace Catalyst.Core.UnitTests.Utils
{
    public sealed class TtlChangeTokenProviderTests : IDisposable
    {
        private readonly ManualResetEvent _manualResetEvent;

        public TtlChangeTokenProviderTests()
        {
            _manualResetEvent = new ManualResetEvent(false);
        }

        [Theory]
        [InlineData(500)]
        [InlineData(1000)]
        public void Can_Hit_Callback_When_Expired(int timeInMs)
        {
            var changeProvider = new TtlChangeTokenProvider(timeInMs);
            var changeToken = changeProvider.GetChangeToken();
            changeToken.RegisterChangeCallback(o => { _manualResetEvent.Set(); }, null);
            var signaled = _manualResetEvent.WaitOne();
            signaled.Should().BeTrue();
        }

        public void Dispose()
        {
            _manualResetEvent.Dispose();
        }
    }
}
