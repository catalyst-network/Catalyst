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
using System.Numerics;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class IdConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case int typedValue:
                    writer.WriteRawValue(typedValue.ToString());
                    break;
                case long typedValue:
                    writer.WriteRawValue(typedValue.ToString());
                    break;
                case BigInteger typedValue:
                    writer.WriteRawValue(typedValue.ToString());
                    break;
                case string typedValue:
                    writer.WriteRawValue(typedValue);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Integer:
                    return reader.Value;
                case JsonToken.String:
                    return reader.Value as string;
                default:
                    throw new NotSupportedException($"{reader.TokenType}");
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
    
    public class JsonRpcRequest
    {
        public string JsonRpc { get; set; }
        public string Method { get; set; }
        
        [JsonProperty(Required = Required.Default)]
        public string[] Params { get; set; }
        
        [JsonConverter(typeof(IdConverter))]
        public object Id { get; set; }

        public override string ToString()
        {
            var paramsString = Params == null ? "" : $"{string.Join(",", Params)}";
            return $"ID {Id}, version {JsonRpc}, {Method}({paramsString})";
        }
    }
}
