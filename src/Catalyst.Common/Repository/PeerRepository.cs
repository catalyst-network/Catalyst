using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.P2P;
using SharpRepository.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Catalyst.Common.Repository
{
    public class PeerRepository : RepositoryWrapper<Peer, string>, IPeerRepository
    {
        public PeerRepository(IRepository<Peer, string> repository) : base(repository)
        {
        }
    }
}
