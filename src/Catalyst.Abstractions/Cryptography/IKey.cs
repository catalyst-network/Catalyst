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

using MultiFormats;

namespace Catalyst.Abstractions.Cryptography
{
    /// <summary>
    ///   Information about a cryptographic key.
    /// </summary>
    public interface IKey
    {
        /// <summary>
        ///   Unique identifier.
        /// </summary>
        /// <value>
        ///   The <see cref="MultiHash"/> of the key's public key.
        /// </value>
        MultiHash Id { get; }

        /// <summary>
        ///   The locally assigned name to the key.
        /// </summary>
        /// <value>
        ///   The name is only unique within the local peer node. The
        ///   <see cref="Id"/> is universally unique.
        /// </value>
        string Name { get; }
    }
}
