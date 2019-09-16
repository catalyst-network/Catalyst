#region LICENSE



#endregion

using Catalyst.Abstractions.Hashing;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using SimpleBase;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var hash = AsMultihash(content);
            var result = Base32.Rfc4648.Encode(hash.ToBytes(), false).ToLowerInvariant();
            return result;
        }

        public byte[] ComputeHash(IEnumerable<byte> content)
        {
            return Multihash.Sum(_multihashAlgorithm.Code, content.ToArray());
        }

        public bool IsValidHash(IEnumerable<byte> content)
        {
            try
            {
                _ = AsMultihash(content);
            } catch(Exception)
            {
                return false;
            }

            return true;
        }

        private Multihash AsMultihash(IEnumerable<byte> bytes)
        {
            var array = bytes as byte[] ?? bytes.ToArray();
            return Multihash.Decode(array);
        }
    }
}
