#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Catalyst.Core.Lib.Util;
using Catalyst.Core.Modules.Authentication.Models;
using Catalyst.Protocol.Peer;
using SharpRepository.Repository;
using System.Linq;

namespace Catalyst.Core.Modules.Authentication.Repository
{
    public sealed class AuthCredentialRepository : IAuthCredentialRepository
    {
        private readonly IRepository<AuthCredentials, string> _repository;

        public AuthCredentialRepository(IRepository<AuthCredentials, string> repository)
        {
            _repository = repository;
        }

        public void Add(AuthCredentials authCredentials)
        {
            _repository.Add(authCredentials);
        }

        public bool TryFind(PeerId peerIdentifier, out AuthCredentials authCredentials)
        {
            return _repository.TryFind(t => t.IpAddress.Equals(peerIdentifier.Ip.ToString()) &&
                t.PublicKey.KeyToBytes().SequenceEqual(peerIdentifier.PublicKey), out authCredentials);
        }

        public void Dispose()
        {
            _repository.Dispose();
        }
    }
}
