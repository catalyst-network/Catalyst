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
using Catalyst.Abstractions.IO.Observers;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.P2P.Repository;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Wire;
using MultiFormats;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.IO.Observers
{
    /**
     * Receives candidate delta broadcasts from producers in validation pool.
     * Validates hash and then adds to IDeltaVoter
     */
    public sealed class CandidateDeltaObserver : BroadcastObserverBase<CandidateDeltaBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaVoter _deltaVoter;
        private readonly IHashProvider _hashProvider;
        private readonly ILedgerInformation _ledgerInformation;

        public CandidateDeltaObserver(IDeltaVoter deltaVoter, IPeerRepository peerRepository, IHashProvider provider, ILedgerInformation ledgerInformation, ILogger logger)
            : base(logger)
        {
            _deltaVoter = deltaVoter;
            _hashProvider = provider;
            _ledgerInformation = ledgerInformation;
        }

        public override void HandleBroadcast(ProtocolMessage message)
        {
            try
            {
                if (!_ledgerInformation.IsConcensusReady)
                {
                    return;
                }

                var multiAddress = new MultiAddress(message.Address);
                Logger.Verbose("received {message} from {port}", message.CorrelationId.ToCorrelationId(),
                    multiAddress.GetPort());

                // @TODO here we use the protobuff message to parse rather than using the CandidateDeltaBroadcastDao
                /////////////////////////////////////////////////////////////////////////////////////////////////
                var deserialized = message.FromProtocolMessage<CandidateDeltaBroadcast>();
                var previousDeltaDfsHashCid = deserialized.PreviousDeltaDfsHash.ToByteArray().ToCid();
                /////////////////////////////////////////////////////////////////////////////////////////////////

                if (!_hashProvider.IsValidHash(previousDeltaDfsHashCid.Hash.ToArray()))
                {
                    Logger.Error("PreviousDeltaDfsHash is not a valid hash");
                    return;
                }

                /////////////////////////////////////////////////////////////////////////////////////////////////
                var hashCid = deserialized.Hash.ToByteArray().ToCid();
                /////////////////////////////////////////////////////////////////////////////////////////////////

                if (!_hashProvider.IsValidHash(hashCid.Hash.ToArray()))
                {
                    Logger.Error("Hash is not a valid hash");
                    return;
                }

                if (deserialized.IsValid())
                {
                    _deltaVoter.OnNext(deserialized);
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception,
                    $"Failed to process candidate delta broadcast {message.ToJsonString()}.");
            }
        }
    }
}
