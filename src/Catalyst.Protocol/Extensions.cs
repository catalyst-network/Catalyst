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
        private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

        /// <summary>
        /// Serializes a message using a pooled array.
        /// </summary>
        /// <param name="message">The message to be serialized.</param>
        /// <returns>By-ref struct that should be disposed at the end of the usage.</returns>
        public static PooledSerializedMessage SerializeToPooledBytes(this IMessage message)
        {
            var messageSize = message.CalculateSize();
            var array = Pool.Rent(messageSize);

            using (CodedOutputStream output = new(array))
            {
                message.WriteTo(output);
            }

            return new PooledSerializedMessage(array, messageSize);
        }

        /// <summary>
        /// The serialized message wrapped in a scope, a disposable-like by-ref struct.
        /// </summary>
        public ref struct PooledSerializedMessage
        {
            private readonly byte[] _array;
            private readonly int _size;

            public PooledSerializedMessage(byte[] array, int size)
            {
                _array = array;
                _size = size;
            }

            public ReadOnlySpan<byte> Span => _array.AsSpan(0, _size);

            /// <summary>
            /// Returns the underlying pool to array.
            /// </summary>
            public void Dispose() => Pool.Return(_array);
        }
    }
}
