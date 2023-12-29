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

using System.Buffers;
using Google.Protobuf;
using MultiFormats;

namespace Catalyst.Abstractions.Hashing
{
    public static class HashExtensions
    {
        /// <summary>
        /// Serializes the <paramref name="message"/>, appends suffix and returns the hash of it.
        /// </summary>
        /// <param name="provider">The hash provider.</param>
        /// <param name="message">The protocol message.</param>
        /// <param name="suffix">The suffix that should be appended to the message. </param>
        /// <returns></returns>
        public static MultiHash ComputeMultiHash(this IHashProvider provider, IMessage message, byte[] suffix)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            var calculateSize = message.CalculateSize();

            var required = calculateSize + suffix.Length;
            var array = ArrayPool<byte>.Shared.Rent(required);

            try
            {
                using (var output = new CodedOutputStream(array))
                {
                    message.WriteTo(output);
                }

                suffix.CopyTo(array, calculateSize);

                var result = provider.ComputeMultiHash(array, 0, required);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        /// <summary>
        /// Serializes the <paramref name="message"/> returns the hash of it.
        /// </summary>
        /// <param name="provider">The hash provider.</param>
        /// <param name="message">The protocol message.</param>
        /// <returns></returns>
        public static MultiHash ComputeMultiHash(this IHashProvider provider, IMessage message)
        {
            ProtoPreconditions.CheckNotNull(message, nameof(message));
            var required = message.CalculateSize();

            var array = ArrayPool<byte>.Shared.Rent(required);

            try
            {
                using (var output = new CodedOutputStream(array))
                {
                    message.WriteTo(output);
                }

                var result = provider.ComputeMultiHash(array, 0, required);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }
}
