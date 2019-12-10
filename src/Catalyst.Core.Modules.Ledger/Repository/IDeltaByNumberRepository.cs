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

using LibP2P;

namespace Catalyst.Core.Modules.Ledger.Repository
{
    public interface IDeltaByNumberRepository
    {
        /// <summary>
        /// Tries to find the mapping between the <paramref name="deltaNumber"/> and the <see cref="deltaHash"/>.
        /// </summary>
        /// <returns>Whether the mapping was found.</returns>
        bool TryFind(long deltaNumber, out Cid deltaHash);

        /// <summary>
        /// Maps the <paramref name="deltaNumber"/> to <paramref name="deltaHash"/>.
        /// </summary>
        /// <param name="deltaNumber"></param>
        /// <param name="deltaHash"></param>
        void Map(long deltaNumber, Cid deltaHash);
    }
}
