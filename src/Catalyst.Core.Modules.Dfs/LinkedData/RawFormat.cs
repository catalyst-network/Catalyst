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

using PeterO.Cbor;

namespace Catalyst.Core.Modules.Dfs.LinkedData
{
    /// <summary>
    ///   Unlinked data.
    /// </summary>
    public class RawFormat : ILinkedDataFormat
    {
        /// <inheritdoc />
        public CBORObject Deserialise(byte[] data)
        {
            return CBORObject.NewMap().Add("data", data)
               .Add("links", null);
        }

        /// <inheritdoc />
        public byte[] Serialize(CBORObject data) { return data["data"].GetByteString(); }
    }
}
