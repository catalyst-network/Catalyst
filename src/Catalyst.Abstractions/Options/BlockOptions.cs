#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

namespace Catalyst.Abstractions.Options
{
    /// <summary>
    ///   Configuration options for a <see cref="IBlockApi">block service</see>.
    /// </summary>
    /// <seealso cref="DfsOptions"/>
    public class BlockOptions
    {
        /// <summary>
        ///   Determines if an inline CID can be created.
        /// </summary>
        /// <value>
        ///   Defaults to <b>false</b>.
        /// </value>
        /// <remarks>
        ///   An "inline CID" places the content in the CID not in a seperate block.
        ///   It is used to speed up access to content that is small.
        /// </remarks>
        public bool AllowInlineCid { get; set; }

        /// <summary>
        ///   Used to determine if the content is small enough to be inlined.
        /// </summary>
        /// <value>
        ///   The maximum number of bytes for content that will be inlined.
        ///   Defaults to 64.
        /// </value>
        public int InlineCidLimit { get; set; } = 64;

        /// <summary>
        ///   The maximun length of data block.
        /// </summary>
        /// <value>
        /// </value>
        ///   1MB (1024 * 1024)
        public int MaxBlockSize { get; } = 1024 * 1024;
    }
}
