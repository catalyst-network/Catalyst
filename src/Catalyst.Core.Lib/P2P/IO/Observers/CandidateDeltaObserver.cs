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
using System.Linq;
using Catalyst.Common.Interfaces.IO.Messaging.Dto;
using Catalyst.Common.Interfaces.IO.Observers;
using Catalyst.Common.Interfaces.Modules.Consensus.Deltas;
using Catalyst.Common.IO.Observers;
using Catalyst.Common.Util;
using Catalyst.Protocol;
using Catalyst.Protocol.Common;
using Catalyst.Protocol.Deltas;
using Multiformats.Hash;
using Serilog;
using BinaryEncoding;

namespace Catalyst.Core.Lib.P2P.IO.Observers
{
    public class CandidateDeltaObserver : BroadcastObserverBase<CandidateDeltaBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaVoter _deltaVoter;

        public CandidateDeltaObserver(IDeltaVoter deltaVoter, ILogger logger) 
            : base(logger)
        {
            _deltaVoter = deltaVoter;
        }

        public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
        {
            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<CandidateDeltaBroadcast>();

                var byteArray = deserialised.PreviousDeltaDfsHash.ToByteArray();
                var offset = Binary.Varint.Read(byteArray, 0, out uint code);
                offset += Binary.Varint.Read(byteArray, offset, out uint length);

                Multihash.Cast(byteArray
                   .Concat(ByteUtil.InitialiseEmptyByteArray(256)).Take(256).ToArray());
                Multihash.Cast(deserialised.Hash.ToByteArray());
                _deltaVoter.OnNext(deserialised);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, $"Failed to process candidate delta broadcast {messageDto.Payload.ToJsonString()}.");
            }
        }
    }
}
