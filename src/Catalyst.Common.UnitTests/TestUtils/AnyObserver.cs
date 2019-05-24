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
using Catalyst.Common.Util;
using Catalyst.Common.Interfaces.IO.Inbound;
using Catalyst.Protocol.Common;
using Serilog;

namespace Catalyst.Common.UnitTests.TestUtils
{
    public sealed class AnySignedMessageObserver : IObserver<IChanneledMessage<AnySigned>>
    {
        private readonly ILogger _logger;

        public AnySignedMessageObserver(int index, ILogger logger)
        {
            _logger = logger;
            Index = index;
        }

        public IChanneledMessage<AnySigned> Received { get; private set; }
        public int Index { get; }

        public void OnCompleted() { _logger.Debug($"observer {Index} done"); }
        public void OnError(Exception error) { _logger.Debug($"observer {Index} received error : {error.Message}"); }

        public void OnNext(IChanneledMessage<AnySigned> value)
        {
            if (value == NullObjects.ChanneledAnySigned)
            {
                return;
            }

            _logger.Debug($"observer {Index} received message of type {value?.Payload?.TypeUrl ?? "(null)"}");
            Received = value;
        }
    }
}
