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

using Catalyst.Common.Interfaces.Attributes;
using SharpRepository.Repository;

namespace Catalyst.Common.Interfaces.Rpc.Authentication
{
    public interface IAuthCredentials : IAuditable
    {
        /// <summary>Gets or sets the public key.</summary>
        /// <value>The public key.</value>
        [RepositoryPrimaryKey(Order = 1)]
        string PublicKey { get; set; }

        /// <summary>Gets or sets the ip address.</summary>
        /// <value>The ip address.</value>
        [RepositoryPrimaryKey(Order = 2)]
        string IpAddress { get; set; }
    }
}
