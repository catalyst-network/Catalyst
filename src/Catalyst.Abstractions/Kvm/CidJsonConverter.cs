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
using Lib.P2P;
using MultiFormats;
using Nethermind.Core.Crypto;
using Nethermind.Evm.Tracing.GethStyle.JavaScript;
using Nethermind.Serialization.Json;
using Newtonsoft.Json;

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
            var hash256 = Converter.ReadJson(reader, typeof(Hash256), ToKeccak(existingValue), hasExistingValue, serializer);
            if (hash256 == null)
            {
                return null;
            }

            return new Cid
            {
                Version = 1,
                Encoding = "base32",
                ContentType = "dag-pb",
                Hash = new MultiHash("blake2b-256", hash256.ToBytes())
            };
        }

        static Hash256 ToKeccak(Cid value) { return value == null ? null : new Hash256(value.Hash.Digest); }
    }
}
