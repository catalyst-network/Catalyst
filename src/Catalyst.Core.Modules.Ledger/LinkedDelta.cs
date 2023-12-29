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

// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Catalyst.Core.Modules.Ledger
{
    /// <summary>
    /// A delta which is linked both ways, to its predecessor (like all normal deltas), but also to its
    /// successor. This is meant to be used during re-synchronisation of the ledger.
    /// </summary>
    public class ChainedDeltaHash
    {
        public ChainedDeltaHash(byte[] previousDfsHash, byte[] dfsHash, byte[] nextDeltaDfsHash)
        {
            PreviousDfsHash = previousDfsHash;
            DfsHash = dfsHash;
            NextDeltaDfsHash = nextDeltaDfsHash;
        }

        /// <summary>
        /// The hash or address of the predecessor of this delta on the Dfs.
        /// </summary>
        public byte[] PreviousDfsHash { get; }

        /// <summary>
        /// The hash or address of this delta on the Dfs.
        /// </summary>
        public byte[] DfsHash { get; }

        /// <summary>
        /// The hash or address of the successor of this delta on the Dfs. 
        /// </summary>
        private byte[] NextDeltaDfsHash { get; }
    }
}
