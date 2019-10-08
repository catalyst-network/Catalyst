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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Catalyst.Abstractions.Hashing;
using Ipfs;
using Ipfs.Registry;

namespace Catalyst.Core.Modules.Hashing
{
    public class HashProvider : IHashProvider
    {
        public HashingAlgorithm HashingAlgorithm { set; get; }

        public HashProvider(HashingAlgorithm hashingAlgorithm) { HashingAlgorithm = hashingAlgorithm; }

        public MultiHash Cast(byte[] data)
        {
            return new MultiHash(data);
        }

        public MultiHash ComputeUtf8MultiHash(string data) { return ComputeMultiHash(Encoding.UTF8.GetBytes(data)); }

        public MultiHash ComputeMultiHash(IEnumerable<byte> data) { return ComputeMultiHash(data.ToArray()); }

        public MultiHash ComputeMultiHash(byte[] data) { return MultiHash.ComputeHash(data, HashingAlgorithm.Name); }

        public MultiHash ComputeMultiHash(Stream data) { return MultiHash.ComputeHash(data, HashingAlgorithm.Name); }
    }
}
