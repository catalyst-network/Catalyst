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

using Ipfs.HttpGateway;

namespace Catalyst.Abstractions.Dfs
{
    /// <summary>
    ///   Provides read-only access to the distribute files system via HTTP.
    /// </summary>
    /// <seealso cref="IDfs"/>
    public interface IDfsGateway
    {
        GatewayHost Gateway { get; }
        
        /// <summary>
        ///   Gets the URL of DFS content.
        /// </summary>
        /// <param name="id">The unique ID of the content in the DFS.</param>
        /// <returns>
        ///   The URL of the DFS <paramref name="id"/>.
        /// </returns>
        string ContentUrl(string id);
    }
}
