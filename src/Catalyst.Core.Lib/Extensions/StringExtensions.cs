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
using System.Net;
using System.Text;
using Catalyst.Core.Lib.Util;
using Catalyst.Protocol.Peer;
using Google.Protobuf;
using Multiformats.Base;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Lib.Extensions
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
        
        public static byte[] ToUtf8Bytes(this string @string) => Encoding.UTF8.GetBytes(@string);

        public static Multihash ComputeUtf8Multihash(this string content, IMultihashAlgorithm algorithm)
        {
            var multihash = content.ToUtf8Bytes().ComputeMultihash(algorithm);
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

        public static PeerId BuildPeerIdFromBase32CrockfordKey(this string base32CrockfordKey, IPEndPoint ipEndPoint)
        {
            return BuildPeerIdFromBase32CrockfordKey(base32CrockfordKey, ipEndPoint.Address, ipEndPoint.Port);
        }

        public static PeerId BuildPeerIdFromBase32CrockfordKey(this string base32CrockfordKey, IPAddress ipAddress, int port)
        {
            return base32CrockfordKey.KeyToBytes().BuildPeerIdFromPublicKey(ipAddress, port);
        }

        public static T ParseHexStringTo<T>(this string hex16Pid) where T : IMessage<T>, new()
        {
            var parser = new MessageParser<T>(() => new T());
            return parser.ParseFrom((SimpleBase.Base16.Decode(hex16Pid)));
        }
    }
}
