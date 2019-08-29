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
using Catalyst.Abstractions.Attributes;
using Catalyst.Abstractions.Repository;

namespace Catalyst.Abstractions.P2P.Models
{
    public interface IPeer : IDocument, IAuditable
    {
        /// <summary>Gets the reputation.</summary>
        /// <value>The reputation.</value>
        int Reputation { get; set; }

        /// <summary>Gets the blacklisting state of the peer.</summary>
        /// <value>The black listing flag.</value>
        bool BlackListed { get; set; }

        /// <summary>Gets the last seen.</summary>
        /// <value>The last seen.</value>
        DateTime LastSeen { get; }

        /// <summary>Gets the peer identifier.</summary>
        /// <value>The peer identifier.</value>
        IPeerIdentifier PeerIdentifier { get; }

        /// <summary>Gets a value indicating whether this instance is awol peer.</summary>
        /// <value><c>true</c> if this instance is awol peer; otherwise, <c>false</c>.</value>
        bool IsAwolPeer { get; }

        /// <summary>Gets the inactive for.</summary>
        /// <value>The inactive for.</value>
        TimeSpan InactiveFor { get; }

        /// <summary>
        /// </summary>
        void Touch();
    }
}
