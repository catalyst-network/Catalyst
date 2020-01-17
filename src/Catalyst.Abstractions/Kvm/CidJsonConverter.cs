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
using LibP2P;
using Nethermind.Core.Crypto;
using Nethermind.Core.Json;
using Newtonsoft.Json;
using TheDotNetLeague.MultiFormats.MultiHash;

namespace Catalyst.Abstractions.Kvm
{
    public class CidJsonConverter : JsonConverter<Cid>
    {
        static readonly KeccakConverter Converter = new KeccakConverter();

        public override void WriteJson(JsonWriter writer, Cid value, JsonSerializer serializer)
        {
            Converter.WriteJson(writer, ToKeccak(value), serializer);
        }

        public override Cid ReadJson(JsonReader reader, Type objectType, Cid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var keccak = Converter.ReadJson(reader, typeof(Keccak), ToKeccak(existingValue), hasExistingValue, serializer);
            if (keccak == null)
            {
                return null;
            }

            return new Cid
            {
                Version = 1,
                Encoding = "base32",
                ContentType = "raw",
                Hash = new MultiHash("blake2b-256", keccak.Bytes)
            };
        }

        static Keccak ToKeccak(Cid value) { return value == null ? null : new Keccak(value.Hash.Digest); }
    }
}
