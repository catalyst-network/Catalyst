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
using Lib.P2P;

namespace Catalyst.Abstractions.Dfs.BlockExchange
{
    /// <summary>
    ///   The content addressable ID related to an event. 
    /// </summary>
    /// <see cref="Cid"/>
    /// <see>
    ///     <cref>Catalyst.Core.Modules.Dfs.BlockExchange.BitswapService.BlockNeeded</cref>
    /// </see>
    public sealed class CidEventArgs : EventArgs
    {
        /// <summary>
        ///   The content addressable ID. 
        /// </summary>
        /// <value>
        ///   The unique ID of the block.
        /// </value>
        public Cid Id { get; set; }
    }
}
