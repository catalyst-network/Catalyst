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

namespace Catalyst.Core.Modules.Dfs.WebApi.V0.Dto
{
    /// <summary>
    ///   A cryptographic key.
    /// </summary>
    public class CryptoKeyRenameDto
    {
        /// <summary>
        ///   The key's local name.
        /// </summary>
        public string Was { set; get; }

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Now { set; get; }

        /// <summary>
        ///   The key's global unique ID.
        /// </summary>
        public string Id { set; get; }

        /// <summary>
        ///   Indicates that a existing key was overwritten.
        /// </summary>
        public bool Overwrite { set; get; }
    }
}
