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
using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.IO.Messaging.Dto;
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Abstractions.Sync.Interfaces;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Core.Modules.Sync.Modal;
using Catalyst.Protocol.Wire;
using Dawn;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.IO.Observers
{
    public class DeltaDfsHashObserver : BroadcastObserverBase<DeltaDfsHashBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaHashProvider _deltaHashProvider;
        private readonly IDeltaHeightWatcher _deltaHeightWatcher;

        private readonly SyncState _syncState;
        private readonly IPeerRepository _peerRepository;

        public DeltaDfsHashObserver(IDeltaHashProvider deltaHashProvider,
            SyncState syncState,
            IPeerRepository peerRepository,
            ILogger logger)
            : base(logger)
        {
            Guard.Argument(peerRepository, nameof(peerRepository)).NotNull();

            _syncState = syncState;
            _deltaHashProvider = deltaHashProvider;
            _peerRepository = peerRepository;
        }

        public override void HandleBroadcast(IObserverDto<ProtocolMessage> messageDto)
        {
            if (!_syncState.IsSynchronized)
            {
                //_deltaHeightWatcher.LatestDeltaHash = deserialised.DeltaDfsHash;
                return;
            }

            try
            {
                var deserialised = messageDto.Payload.FromProtocolMessage<DeltaDfsHashBroadcast>();

                var previousHash = deserialised.PreviousDeltaDfsHash.ToByteArray().ToCid();
                if (previousHash == null)
                {
                    Logger.Error("PreviousDeltaDfsHash is not a valid hash");
                    return;
                }

                var newHash = deserialised.DeltaDfsHash.ToByteArray().ToCid();
                if (newHash == null)
                {
                    Logger.Error("DeltaDfsHash is not a valid hash");
                    return;
                }

                var messagePoaNode = _peerRepository.GetPeersByIpAndPublicKey(messageDto.Payload.PeerId.Ip, messageDto.Payload.PeerId.PublicKey).FirstOrDefault();
                if (messagePoaNode == null)
                {
                    Logger.Error($"Message from IP address '{messageDto.Payload.PeerId.Ip}' with public key '{messageDto.Payload.PeerId.PublicKey}' is not found in producer node list.");
                    return;
                }

                _deltaHashProvider.TryUpdateLatestHash(previousHash, newHash);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Failed to update latest delta hash from incoming broadcast message.");
            }
        }
    }
}
