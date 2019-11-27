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

using System;
using System.Buffers;
using Google.Protobuf;

namespace Catalyst.Protocol
{
    /// <summary>
    /// Provides various extensions for serialization.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Serializes a message using the passed array pool.
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="message"></param>
        /// <param name="arrayToReturn"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> Serialize(this ArrayPool<byte> pool, IMessage message, out byte[] arrayToReturn)
        {
            var messageSize = message.CalculateSize();
            arrayToReturn = pool.Rent(messageSize);

            using (var output = new CodedOutputStream(arrayToReturn))
            {
                message.WriteTo(output);
            }

            return arrayToReturn.AsSpan(0, messageSize);
        }
    }
}
