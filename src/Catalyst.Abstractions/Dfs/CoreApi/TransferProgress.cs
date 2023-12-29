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

using System;

namespace Catalyst.Abstractions.Dfs.CoreApi
{
    /// <summary>
    ///   Reports the <see cref="IProgress{T}">progress</see> of
    ///   a transfer operation.
    /// </summary>
    public class TransferProgress
    {
        /// <summary>
        ///   The name of the item being trasfered.
        /// </summary>
        /// <value>
        ///   Typically, a relative file path.
        /// </value>
        public string Name;

        /// <summary>
        ///   The cumuative number of bytes transfered for
        ///   the <see cref="Name"/>.
        /// </summary>
        public ulong Bytes;
    }
}
