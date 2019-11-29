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
