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

using Catalyst.Abstractions.Consensus.Deltas;
using Catalyst.Abstractions.Ledger;
using Catalyst.Abstractions.Validators;
using Catalyst.Core.Abstractions.Sync;
using Catalyst.Protocol.Deltas;
using Lib.P2P;
using System;

namespace Catalyst.Core.Modules.Ledger
{
    public class LedgerInformation : ILedgerInformation
    {
        public long DeltaNumber { private set; get; }
        public Cid DeltaHash { private set; get; }
        public Delta LatestDelta { private set; get; }
        public bool IsConcensusReady { private set; get; } = false;

        private SyncState _syncState;
        private IDeltaCache _deltaCache;
        private IDeltaHashProvider _deltaHashProvider;
        private IValidatorSetStore _validatorSetStore;

        public LedgerInformation(IDeltaCache deltaCache, IDeltaHashProvider deltaHashProvider, IValidatorSetStore validatorSetStore, SyncState syncState)
        {
            _syncState = syncState;
            _deltaCache = deltaCache;
            _deltaHashProvider = deltaHashProvider;
            _validatorSetStore = validatorSetStore;

            UpdateLatestDelta(deltaCache.GenesisHash);
            _deltaHashProvider.DeltaHashUpdates.Subscribe(UpdateLatestDelta);
        }

        public void UpdateLatestDelta(Cid deltaHash)
        {
            _deltaCache.TryGetOrAddConfirmedDelta(deltaHash, out var latestDelta);
            LatestDelta = latestDelta;
            DeltaNumber = LatestDelta.DeltaNumber;
            DeltaHash = deltaHash;

            if (!IsConcensusReady)
            {
                IsConcensusReady = _validatorSetStore.Get(DeltaNumber).Equals(_validatorSetStore.Get((long)_syncState.HighestBlock));
            }
        }
    }
}
