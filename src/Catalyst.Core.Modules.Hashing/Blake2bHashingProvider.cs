#region LICENSE



#endregion

using Catalyst.Abstractions.Hashing;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using SimpleBase;
using System;
using System.Collections.Generic;
using System.Linq;
using Multiformats.Base;

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
namespace Catalyst.Core.Modules.Hashing
{
    public class Blake2bHashingProvider : IHashProvider
    {
        private readonly IMultihashAlgorithm _multihashAlgorithm;

        public Blake2bHashingProvider(IMultihashAlgorithm multihashAlgorithm)
        {
            _multihashAlgorithm = multihashAlgorithm;
        }

        public string AsBase32(IEnumerable<byte> content)
        {
            var contentList = content.ToList();
            var hash = ComputeHash(contentList);
            var multiHash = AsMultihash(hash);
            var result = multiHash.ToString(MultibaseEncoding.Base32Lower);
            return result;
        }
        
        public byte[] ComputeHash(IEnumerable<byte> content)
        {
            return Multihash.Sum(_multihashAlgorithm.Code, content.ToArray()).ToBytes();
        }

        public byte[] GetBase32HashBytes(string hash)
        {
            return Multihash.Parse(hash, MultibaseEncoding.Base32Lower).ToBytes();
        }

        public bool IsValidHash(IEnumerable<byte> content)
        {
            try
            {
                _ = AsMultihash(content);
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Simply takes some bytes, and use the <see cref="_multihashAlgorithm"/> to compute the hash
        /// for this content. The hash is returned raw, <see cref="ComputeHash"/> to get the bytes
        /// wrapped in a Multihash envelope.
        /// </summary>
        /// <param name="bytes">The content for which the hash will be calculated.</param>
        /// <returns>The raw result of the hashing operation, as a byte array.</returns>
        private Multihash AsMultihash(IEnumerable<byte> bytes)
        {
            var array = bytes as byte[] ?? bytes.ToArray();
            return Multihash.Decode(array);
        }
    }
}
