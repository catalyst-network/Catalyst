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

using Catalyst.Abstractions.Dfs.CoreApi;

namespace Catalyst.Abstractions.Dfs
{
    /// <summary>
    ///   Content that has an associated name.
    /// </summary>
    /// <seealso cref="INameApi"/>
    public class NamedContent
    {
        /// <summary>
        ///   Path to the name.
        /// </summary>
        /// <value>
        ///   Typically <c>/ipns/...</c>.
        /// </value>
        public string NamePath { get; set; }

        /// <summary>
        ///   Path to the content.
        /// </summary>
        /// <value>
        ///   Typically <c>/ipfs/...</c>.
        /// </value>
        public string ContentPath { get; set; }
    }
}
