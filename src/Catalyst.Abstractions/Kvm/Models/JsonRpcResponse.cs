#region LICENSE

/**
* Copyright (c) 2024 Catalyst Network
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

using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class Error
    {
        [JsonProperty(PropertyName = "code")]
        public int Code { get; set; }
        
        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
        
        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
    }

    public class JsonRpcResponse
    {
        public JsonRpcResponse() { }
        
        public JsonRpcResponse(JsonRpcRequest request, object result)
        {
            JsonRpc = request.JsonRpc;
            Id = request.Id;
            Result = result;
        }
        
        [JsonProperty(PropertyName = "jsonrpc", Order = 1)]
        public string JsonRpc { get; set; }

        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Include, Order = 2)]
        public object Result { get; set; }
        
        [JsonConverter(typeof(IdConverter))]
        [JsonProperty(PropertyName = "id", Order = 0)]
        public object Id { get; set; }
    }
    
    public class JsonRpcErrorResponse : JsonRpcResponse
    {
        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
        public new object Result { get; set; }
        
        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore, Order = 3)]
        public Error Error { get; set; }
    }

    public class JsonRpcResponse<T>
    {
        [JsonProperty(PropertyName = "jsonrpc", Order = 1)]
        public string JsonRpc { get; set; }

        [JsonProperty(PropertyName = "result", NullValueHandling = NullValueHandling.Ignore, Order = 2)]
        public T Result { get; set; }

        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore, Order = 3)]
        public Error Error { get; set; }
        
        [JsonConverter(typeof(IdConverter))]
        [JsonProperty(PropertyName = "id", Order = 0)]
        public object Id { get; set; }
    }
}
