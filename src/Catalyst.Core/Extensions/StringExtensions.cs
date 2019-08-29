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

using System.IO;
using System.Text;
using Multiformats.Base;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Extensions
{
    public static class StringExtensions
    {
        public static Stream ToMemoryStream(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static Multihash ComputeUtf8Multihash(this string content, IMultihashAlgorithm algorithm)
        {
            var multihash = Encoding.UTF8.GetBytes(content).ComputeMultihash(algorithm);
            return multihash;
        }

        public static Multihash FromBase32Address(this string address)
        {
            var success = Multihash.TryParse(address, MultibaseEncoding.Base32Lower, out var multihash);
            if (!success)
            {
                throw new InvalidDataException($"{address} is not valid");
            }
            
            return multihash;
        }
    }
}
