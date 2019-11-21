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
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using SharpRepository.Repository;
using SharpRepository.Repository.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Catalyst.Core.Lib.P2P.Repository
{
    public class PeerRepository : IPeerRepository, IDisposable
    {
        public IRepository<Peer, string> Repository { set; get; }
        public PeerRepository(IRepository<Peer, string> repository)
        {
            Repository = repository;
        }

        public Peer Get(string id)
        {
            return Repository.Get(id);
        }

        public IEnumerable<Peer> GetAll()
        {
            return Repository.GetAll();
        }

        public IEnumerable<Peer> GetActivePeers(int count)
        {
            return Repository.FindAll(new Specification<Peer>(p => !p.IsAwolPeer)).Take(count);
        }

        public IEnumerable<Peer> GetRandomPeers(int count)
        {
            return Repository.AsQueryable().Select(c => c.DocumentId).Shuffle().Take(count).Select(Repository.Get).ToList();
        }

        public IEnumerable<Peer> GetPeersByIpAndPublicKey(ByteString ip, ByteString publicKey)
        {
            return Repository.FindAll(m => m.PeerId.Ip == ip && (publicKey.IsEmpty || m.PeerId.PublicKey == publicKey));
        }

        public void Add(Peer peer)
        {
            Repository.Add(peer);
        }

        public void Add(IEnumerable<Peer> peer)
        {
            Repository.Add(peer);
        }

        public bool Exists(string id)
        {
            return Repository.Exists(id);
        }

        public void Update(Peer peer)
        {
            Repository.Update(peer);
        }

        public uint DeletePeersByIpAndPublicKey(ByteString ip, ByteString publicKey)
        {
            var peerDeletedCount = 0u;
            var peersToDelete = GetPeersByIpAndPublicKey(ip, publicKey);

            foreach (var peerToDelete in peersToDelete)
            {
                Repository.Delete(peerToDelete);
                peerDeletedCount += 1;
            }

            return peerDeletedCount;
        }

        public void Delete(Peer peer)
        {
            Repository.Delete(peer);
        }

        public void Delete(string id)
        {
            Repository.Delete(id);
        }

        public int Count()
        {
            return Repository.Count();
        }

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
