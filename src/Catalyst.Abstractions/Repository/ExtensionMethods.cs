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

using Catalyst.Abstractions.Hashing;
using Catalyst.Protocol.Transaction;
using Nethermind.Core.Crypto;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Abstractions.Repository
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the document id on the basis of <paramref name="entry"/> content.
        /// </summary>
        public static string GetDocumentId(this PublicEntry entry, IHashProvider hashProvider) => hashProvider.ComputeMultiHash(entry).AsDocumentId();

        /// <summary>
        /// Transforms <paramref name="hash"/> to a unified document id.
        /// </summary>
        public static string AsDocumentId(this MultiHash hash) => new Keccak(hash.Digest).AsDocumentId();

        /// <summary>
        /// Transforms <paramref name="keccak"/> to a unified document id.
        /// </summary>
        public static string AsDocumentId(this Keccak keccak) => keccak.ToString(false);
    }
}
