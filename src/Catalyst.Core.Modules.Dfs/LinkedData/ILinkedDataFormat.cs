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

using PeterO.Cbor;

namespace Catalyst.Core.Modules.Dfs.LinkedData
{
    /// <summary>
    ///   A specific format for linked data.
    /// </summary>
    /// <remarks>
    ///   Allows the conversion between the canonincal form of linked data and its binary
    ///   representation in a specific format.
    ///   <para>
    ///   The canonical form is a <see cref="CBORObject"/>.
    ///   </para>
    /// </remarks>
    public interface ILinkedDataFormat
    {
        /// <summary>
        ///   Convert the binary represention into the equivalent canonical form. 
        /// </summary>
        /// <param name="data">
        ///   The linked data encoded in a specific format.
        /// </param>
        /// <returns>
        ///   The canonical representation of the <paramref name="data"/>.
        /// </returns>
        CBORObject Deserialise(byte[] data);

        /// <summary>
        ///   Convert the canonical data into the specific format.
        /// </summary>
        /// <param name="data">
        ///   The canonical data to convert.
        /// </param>
        /// <returns>
        ///   The binary representation of the <paramref name="data"/> encoded
        ///   in the specific format.
        /// </returns>
        byte[] Serialize(CBORObject data);
    }
}
