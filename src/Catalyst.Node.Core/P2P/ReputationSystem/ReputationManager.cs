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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Catalyst.Common.Interfaces.IO.Messaging;
using Catalyst.Common.Interfaces.P2P.ReputationSystem;
using Catalyst.Common.P2P;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.P2P.ReputationSystem
{
    public class ReputationManager : IReputationManager
    {
        public IRepository<Peer> PeerRepository { get; }
        private readonly ReplaySubject<IMessageEvictionEvent> _evictionEvent;
        public IObservable<IMessageEvictionEvent> EvictionEvents => _evictionEvent.AsObservable();

        public ReputationManager(IRepository<Peer> peerRepository)
        {
            PeerRepository = peerRepository;
            _evictionEvent = new ReplaySubject<IMessageEvictionEvent>(0);
        }
    }
}
