#region LICENSE
// 
// Copyright (c) 2019 Catalyst Network
// 
// This file is part of Catalyst.Node <https://github.com/catalyst-network/Catalyst.Node>
// 
// Catalyst.Node is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Catalyst.Node is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Catalyst.Node. If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using Catalyst.Protocol.Transaction;
using Multiformats.Hash;
using Newtonsoft.Json;

namespace Catalyst.Node.Rpc.Client.IO.Observers
{
    //todo: check if that can be useful
    //https://medium.com/google-cloud/making-newtonsoft-json-and-protocol-buffers-play-nicely-together-fe92079cc91c
    internal class DeltaForJsonSerialisation
    {
        public int Version { get; set; }

        [JsonConverter(typeof(MultihashJsonConverter))]
        public Multihash PreviousDeltaDfsHash { get; set; }

        public string MerkleRootAsHex { get; set; }

        public string MerklePodaAsHex { get; set; }

        public DateTime TimeStamp { get; set; }

        public int StandardEntryCount { get; set; }

        public int ConfidentialEntryCount { get; set; }

        [JsonConverter(typeof(CoinbaseEntryJsonConverter))]
        public CoinbaseEntry CoinbaseEntry { get; set; }
    }

    public class MultihashJsonConverter : JsonConverter<Multihash>
    {
        public override void WriteJson(JsonWriter writer, Multihash value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override Multihash ReadJson(JsonReader reader,
            Type objectType,
            Multihash existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var rawHash = (string) reader.Value;
            return Multihash.Parse(rawHash);
        }
    }

    public class CoinbaseEntryJsonConverter : JsonConverter<CoinbaseEntry>
    {
        public override void WriteJson(JsonWriter writer, CoinbaseEntry value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override CoinbaseEntry ReadJson(JsonReader reader,
            Type objectType,
            CoinbaseEntry existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
