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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Catalyst.Abstractions.Hashing;
using Google.Protobuf;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Core.Modules.Hashing
{
    public sealed class HashProvider : IHashProvider
    {
        public HashingAlgorithm HashingAlgorithm { set; get; }

        /// <summary>
        ///     Parse a string representation of a multihash
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static MultiHash Parse(string buffer) { return new MultiHash(buffer); }
        
        /// <summary>
        ///     Parse a byte array representation of a multihash
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static MultiHash Parse(byte[] buffer) { return new MultiHash(buffer); }
        
        /// <summary>
        ///     Parse a memory stream representation of a multihash
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MultiHash Parse(MemoryStream stream) { return new MultiHash(stream); }
        
        /// <summary>
        ///     Parse a protobuff coded input stream representation of a multihash
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static MultiHash Parse(CodedInputStream stream) { return new MultiHash(stream); }
        
        public HashProvider(HashingAlgorithm hashingAlgorithm) { HashingAlgorithm = hashingAlgorithm; }

        public MultiHash Cast(byte[] data) { return CastIfHashIsValid(data); }

        public bool IsValidHash(byte[] data) { return CastIfHashIsValid(data) != null; }

        private MultiHash CastIfHashIsValid(byte[] data)
        {
            try
            {
                var multiHash = new MultiHash(data);
                if (multiHash.Algorithm == HashingAlgorithm && multiHash.Digest.Length == HashingAlgorithm.DigestSize)
                {
                    return multiHash;
                }

                return null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public MultiHash ComputeUtf8MultiHash(string data)
        {
            var byteCount = Encoding.UTF8.GetByteCount(data);

            const int maxBytes = 512;
            if (byteCount < maxBytes)
            {
                Span<byte> span = stackalloc byte[byteCount];
                Encoding.UTF8.GetBytes(data, span);
                return ComputeMultiHash(span);
            }
            else
            {
                using (var owner = MemoryPool<byte>.Shared.Rent(byteCount))
                {
                    var span = owner.Memory.Span;
                    Encoding.UTF8.GetBytes(data, span);
                    return ComputeMultiHash(span);
                }
            }
        }

        public MultiHash ComputeMultiHash(IEnumerable<byte> data)
        {
            return ComputeMultiHash(data.ToArray());
        }

        public MultiHash ComputeMultiHash(byte[] data)
        {
            return MultiHash.ComputeHash(data, HashingAlgorithm.Name);
        }

        public MultiHash ComputeMultiHash(Stream data)
        {
            return MultiHash.ComputeHash(data, HashingAlgorithm.Name);
        }

        public MultiHash ComputeMultiHash(ReadOnlySpan<byte> data)
        {
            var size = HashingAlgorithm.DigestSize;
            var digest = new byte[size];

            using (var hasher = HashingAlgorithm.Hasher())
            {
                var success = hasher.TryComputeHash(data, digest, out var written);

                if (!success || written != size)
                {
                    throw new System.Exception("Hashing failed");
                }

                return new MultiHash(HashingAlgorithm.Name, digest);
            }
        }
    }
}
