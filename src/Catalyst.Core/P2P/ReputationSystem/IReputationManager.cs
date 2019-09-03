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
using Catalyst.Abstractions.P2P.ReputationSystem;
using Catalyst.Core.P2P.Repository;

namespace Catalyst.Core.P2P.ReputationSystem
{
    public interface IReputationManager
    {
        IPeerRepository PeerRepository { get; }

        IObservable<IPeerReputationChange> ReputationEventStream { get; }

        IObservable<IPeerReputationChange> MergedEventStream { get; set; }

        void MergeReputationStream(IObservable<IPeerReputationChange> reputationChangeStream);

        void OnNext(IPeerReputationChange peerReputationChange);
    }
}
