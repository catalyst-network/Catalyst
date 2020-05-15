using System.Collections.Generic;
using System.Linq;
using Mono.Nat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;

namespace Catalyst.Modules.UPnP
{
    public static class PortMappingParser
    {
        public static List<Mapping> ParseJson(string tcp, string udp, string json)
        {
            var mappings = new List<Mapping>();
            var jObject = JObject.Parse(json);
            
            mappings.AddRange(tcp.Split(',')
                .Select(identifier => PortMappingFromJson(jObject, identifier, Protocol.Tcp))
                .Where(mapping => mapping != null)
                .ToList());

            mappings.AddRange(udp.Split(',')
                .Select(identifier => PortMappingFromJson(jObject, identifier, Protocol.Udp))
                .Where(mapping => mapping != null)
                .ToList());
        
            return mappings;
        }

        private static Mapping PortMappingFromJson(JToken jObject, string key, Protocol protocol)
        {
            var portToken = jObject.SelectToken(key);
            var port = portToken?.ToObject<int>();
            
            return port!=null? new Mapping(protocol, port.Value, port.Value) : null;
        }
    }
}
