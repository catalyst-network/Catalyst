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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Extensions;
using Catalyst.Core.IO.Observers;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Serilog;

namespace Catalyst.Core.P2P.IO.Observers
{
    public class FavouriteDeltaObserver : BroadcastObserverBase<FavouriteDeltaBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaElector _deltaElector;

        public FavouriteDeltaObserver(IDeltaElector deltaElector, ILogger logger) 
            : base(logger)
        {
            _deltaElector = deltaElector;
        }

        public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
        {
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<FavouriteDeltaBroadcast>();

                _ = deserialised.Candidate.PreviousDeltaDfsHash.ToByteArray().AsMultihash();
                _ = deserialised.Candidate.Hash.ToByteArray().AsMultihash();
                deserialised.IsValid();
                
                _deltaElector.OnNext(deserialised);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"Failed to process favourite delta broadcast {messageDto.Payload.ToJsonString()}.");
            }
        }
    }
}
