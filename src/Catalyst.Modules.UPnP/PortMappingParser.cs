#region LICENSE

/**
* Copyright (c) 2022 Catalyst Network
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

using System.Collections.Generic;
using System.Linq;
using Mono.Nat;
using Newtonsoft.Json.Linq;

namespace Catalyst.Modules.UPnP
{
    public static class PortMappingParser
    {
        public static List<Mapping> ParseJson(string tcp, string udp, string json)
        {
            List<Mapping> mappings = new();
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
