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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Extensions
{
    public static class BytesExtensions
    {
        public static MemoryStream ToMemoryStream(this byte[] content)
        {
            var memoryStream = new MemoryStream();
            memoryStream.Write(content, 0, content.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Takes some bytes, and use the <see cref="algorithm"/> to compute the hash
        /// for this content. The hash is returned as a multihash, which means it is wrapped with a var-int that
        /// describes its length, and a code that describes the hashing mechanism used,
        /// <see cref="ComputeRawHash"/> to get only the raw bytes.
        /// </summary>
        /// <param name="bytes">The content for which the hash will be calculated.</param>
        /// <param name="algorithm">The hashing algorithm used.</param>
        /// <returns>The raw result of the hashing operation as a Multihash, i.e. enveloped with description metadata.</returns>
        public static Multihash ComputeMultihash(this IEnumerable<byte> bytes, IMultihashAlgorithm algorithm)
        {
            var hashBytes = Multihash.Sum(algorithm.Code, bytes.ToArray());
            return hashBytes;
        }

        /// <summary>
        /// Simply takes some bytes, and use the <see cref="algorithm"/> to compute the hash
        /// for this content. The hash is returned raw, <see cref="ComputeMultihash"/> to get the bytes
        /// wrapped in a Multihash envelope.
        /// </summary>
        /// <param name="bytes">The content for which the hash will be calculated.</param>
        /// <param name="algorithm">The hashing algorithm used.</param>
        /// <returns>The raw result of the hashing operation, as a byte array.</returns>
        public static byte[] ComputeRawHash(this IEnumerable<byte> bytes, IMultihashAlgorithm algorithm)
        {
            var array = bytes as byte[] ?? bytes.ToArray();
            return algorithm.ComputeHash(array);
        }

        public static Multihash AsMultihash(this IEnumerable<byte> bytes)
        {
            var array = bytes as byte[] ?? bytes.ToArray();
            return Multihash.Decode(array);
        }

        public static string AsBase32Address(this IEnumerable<byte> bytes)
        {
            var hash = AsMultihash(bytes);
            var trimmedString = hash.AsBase32Address();
            return trimmedString;
        }
    }
}
