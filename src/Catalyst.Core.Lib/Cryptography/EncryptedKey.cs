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

namespace Catalyst.Core.Lib.Cryptography
{
    /// <summary>
    ///   A private key that is password protected.
    /// </summary>
    public class EncryptedKey
    {
        /// <summary>
        ///   The local name of the key.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///   The unique ID of the key.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///   PKCS #8 container.
        /// </summary>
        /// <value>
        ///   Password protected PKCS #8 structure in the PEM formatw
        /// </value>
        public string Pem { get; set; }
    }
}
