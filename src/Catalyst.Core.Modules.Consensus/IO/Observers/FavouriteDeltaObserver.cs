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
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.IO.Observers;
using Catalyst.Core.Modules.Dfs.Extensions;
using Catalyst.Protocol.Wire;
using Serilog;

namespace Catalyst.Core.Modules.Consensus.IO.Observers
{
    public class FavouriteDeltaObserver : BroadcastObserverBase<FavouriteDeltaBroadcast>, IP2PMessageObserver
    {
        private readonly IDeltaElector _deltaElector;
        private readonly IHashProvider _hashProvider;
        private readonly ILedgerInformation _ledgerInformation;

        public FavouriteDeltaObserver(IDeltaElector deltaElector, IHashProvider hashProvider, ILedgerInformation ledgerInformation, ILogger logger)
            : base(logger)
        {
            _deltaElector = deltaElector;
            _hashProvider = hashProvider;
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

                var deserialized = message.FromProtocolMessage<FavouriteDeltaBroadcast>();

                var previousDeltaDfsHashCid = deserialized.Candidate.PreviousDeltaDfsHash.ToByteArray().ToCid();
                if (!_hashProvider.IsValidHash(previousDeltaDfsHashCid.Hash.ToArray()))
                {
                    Logger.Error("PreviousDeltaDfsHash is not a valid hash");
                    return;
                }

                var hashCid = deserialized.Candidate.Hash.ToByteArray().ToCid();
                if (!_hashProvider.IsValidHash(hashCid.Hash.ToArray()))
                {
                    Logger.Error("Hash is not a valid hash");
                    return;
                }

                deserialized.IsValid();

                _deltaElector.OnNext(deserialized);
            }
            catch (Exception exception)
            {
                Logger.Error(exception,
                    $"Failed to process favourite delta broadcast {message.ToJsonString()}.");
            }
        }
    }
}
