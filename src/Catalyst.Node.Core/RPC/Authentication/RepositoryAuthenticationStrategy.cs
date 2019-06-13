using System;
using System.Collections.Generic;
using System.Text;
using Catalyst.Common.Interfaces.P2P;
using Catalyst.Common.Interfaces.Rpc.Authentication;
using SharpRepository.Repository;

namespace Catalyst.Node.Core.RPC.Authentication
{
    public class RepositoryAuthenticationStrategy : IAuthenticationStrategy
    {
        private IRepository<AuthCredentials> _trustedPeers;

        public RepositoryAuthenticationStrategy(IRepository<AuthCredentials> trustedPeers)
        {
            _trustedPeers = trustedPeers;
        }

        public bool Authenticate(IPeerIdentifier peerIdentifier)
        {
            
        }
    }
}
