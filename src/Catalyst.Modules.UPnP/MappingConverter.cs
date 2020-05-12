using System;
using System.Security.Claims;
using Mono.Nat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catalyst.Modules.UPnP
{
    public class MappingConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Mapping);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            if (!Enum.TryParse((string) jo["Protocol"], out Protocol protocol))
            {
                protocol = Protocol.Tcp;
            }

            var publicPort = (int) jo["PublicPort"];
            var privatePort = (int) jo["PrivatePort"];

            return new Mapping(protocol, privatePort, publicPort);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
