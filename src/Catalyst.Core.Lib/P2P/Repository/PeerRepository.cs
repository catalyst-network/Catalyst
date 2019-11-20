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

using Catalyst.Core.Lib.Config;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Models;
using SharpRepository.Repository;
using SharpRepository.Repository.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Core.Lib.P2P.Repository
{
    public class PeerRepository : IPeerRepository, IDisposable
    {
        public IRepository<Peer> Repository { set; get; }

        public IEnumerable<Peer> GetAll()
        {
            return Repository.GetAll();
        }

        public IEnumerable<Peer> GetActivePeers(int count)
        {
            return Repository.FindAll(new Specification<Peer>(p => !p.IsAwolPeer)).Take(count);
        }

        public void Add(Peer peer)
        {
            Repository.Add(peer);
        }

        public void Update(Peer peer)
        {
            Repository.Update(peer);
        }

        public int Count()
        {
            return Repository.Count();
        }

        public IEnumerable<Peer> GetRandomPeers(int count)
        {
            return null;
            //return Repository.AsQueryable().Select(c => c.DocumentId).Shuffle().Take(count).Select(Repository.Get).Select(p => p.PeerId).ToList();
        }

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
