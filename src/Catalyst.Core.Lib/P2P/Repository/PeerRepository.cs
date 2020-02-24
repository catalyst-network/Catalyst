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

using System.Collections.Generic;
using System.Linq;
using Catalyst.Core.Lib.Extensions;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using SharpRepository.Repository;
using SharpRepository.Repository.Queries;
using SharpRepository.Repository.Specifications;

namespace Catalyst.Core.Lib.P2P.Repository
{
    public class PeerRepository : IPeerRepository
    {
        private readonly IRepository<Peer, string> _repository;
        public PeerRepository(IRepository<Peer, string> repository) { _repository = repository; }

        public Peer Get(string id) { return _repository.Get(id); }

        public Peer Get(PeerId id) {
            return _repository.Find(x => x.PeerId.Equals(id)); 
        }

        public IEnumerable<Peer> GetAll() { return _repository.GetAll(); }

        public IEnumerable<Peer> GetActivePeers(int count)
        {
            return _repository.FindAll(new Specification<Peer>(p => !p.IsAwolPeer)).Take(count);
        }

        public IEnumerable<Peer> GetRandomPeers(int count)
        {
            return _repository.AsQueryable().Select(c => c.DocumentId).Shuffle().Take(count).Select(_repository.Get)
               .ToList();
        }

        public IEnumerable<Peer> GetPeersByIpAndPublicKey(ByteString ip, ByteString publicKey)
        {
            return _repository.FindAll(m =>
                m.PeerId.Ip == ip && (publicKey.IsEmpty || m.PeerId.PublicKey == publicKey));
        }

        public IEnumerable<Peer> TakeHighestReputationPeers(int page, int count)
        {
            return _repository.GetAll(new PagingOptions<Peer, int>(page, count, x => x.Reputation, isDescending: true));
        }

        public void Add(Peer peer) { _repository.Add(peer); }

        public void Add(IEnumerable<Peer> peer) { _repository.Add(peer); }

        public bool Exists(string id) { return _repository.Exists(id); }

        public void Update(Peer peer) { _repository.Update(peer); }

        public uint DeletePeersByIpAndPublicKey(ByteString ip, ByteString publicKey)
        {
            var peerDeletedCount = 0u;
            var peersToDelete = GetPeersByIpAndPublicKey(ip, publicKey);

            foreach (var peerToDelete in peersToDelete)
            {
                _repository.Delete(peerToDelete);
                peerDeletedCount += 1;
            }

            return peerDeletedCount;
        }

        public void Delete(Peer peer) { _repository.Delete(peer); }

        public void Delete(string id) { _repository.Delete(id); }

        public int Count() { return _repository.Count(); }

        public void Dispose() { _repository.Dispose(); }
    }
}
