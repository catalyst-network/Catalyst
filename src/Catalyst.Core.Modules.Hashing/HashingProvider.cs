#region LICENSE

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Catalyst.Abstractions.Hashing;
using Ipfs;
using Ipfs.Registry;
using Multiformats.Hash;

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
    public class HashingProvider : IHashProvider
    {
        public HashingAlgorithm HashingAlgorithm { set; get; }

        public HashingProvider(HashingAlgorithm hashingAlgorithm)
        {
            HashingAlgorithm = hashingAlgorithm;
        }

        public MultiHash ComputeMultiHash(IEnumerable<byte> data)
        {
            return MultiHash.ComputeHash(data.ToArray());
        }

        public MultiHash ComputeMultiHash(Stream data)
        {
            return MultiHash.ComputeHash(data);
        }
    }
}
