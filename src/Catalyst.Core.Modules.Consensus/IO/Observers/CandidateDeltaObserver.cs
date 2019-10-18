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
using Catalyst.Abstractions.Hashing;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Wire;
using LibP2P;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.IO.Observers
{
    public class CandidateDeltaObserver : BroadcastObserverBase<CandidateDeltaBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IHashProvider _hashProvider;

        public CandidateDeltaObserver(IDeltaVoter deltaVoter, IHashProvider provider, ILogger logger)
            : base(logger)
        {
            _deltaVoter = deltaVoter;
            _hashProvider = provider;
        }

        public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
        {
            try
            {
                Logger.Verbose("received {message} from {port}", messageDto.Payload.CorrelationId.ToCorrelationId(),
                    messageDto.Payload.PeerId.Port);
                var deserialized = messageDto.Payload.FromProtocolMessage<CandidateDeltaBroadcast>();

                var previousDeltaDfsHashCid = CidHelper.Cast(deserialized.PreviousDeltaDfsHash.ToByteArray());
                if (!_hashProvider.IsValidHash(previousDeltaDfsHashCid.Hash.ToArray()))
                {
                    Logger.Error("PreviousDeltaDfsHash is not a valid hash");
                    return;
                }

                var hashCid = CidHelper.Cast(deserialized.Hash.ToByteArray());
                if (!_hashProvider.IsValidHash(hashCid.Hash.ToArray()))
                {
                    Logger.Error("Hash is not a valid hash");
                    return;
                }

                deserialized.IsValid();

                _deltaVoter.OnNext(deserialized);
            }
            catch (Exception exception)
            {
                Logger.Error(exception,
                    $"Failed to process candidate delta broadcast {messageDto.Payload.ToJsonString()}.");
            }
        }
    }
}
