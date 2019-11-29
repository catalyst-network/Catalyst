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
using Nethermind.Core.Json;
using Newtonsoft.Json;

namespace Catalyst.Abstractions.Kvm.Models
{
    public class BlockParameterConverter : JsonConverter<BlockParameter>
    {
        private NullableLongConverter _longConverter = new NullableLongConverter();
        
        public override void WriteJson(JsonWriter writer, BlockParameter value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value.Type == BlockParameterType.BlockNumber)
            {
                _longConverter.WriteJson(writer, value.BlockNumber, serializer);
            }

            switch (value.Type)
            {
                case BlockParameterType.Earliest:
                    writer.WriteValue("earliest");
                    break;
                case BlockParameterType.Latest:
                    writer.WriteValue("latest");
                    break;
                case BlockParameterType.Pending:
                    writer.WriteValue("pending");
                    break;
                case BlockParameterType.BlockNumber:
                    throw new InvalidOperationException("block number should be handled separately");
                default:
                    throw new InvalidOperationException("unknown block parameter type");
            }
        }

        public override BlockParameter ReadJson(JsonReader reader, Type objectType, BlockParameter existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            string value = reader.Value as string;
            switch (value)
            {
                case "earliest":
                    return BlockParameter.Earliest;
                case "pending":
                    return BlockParameter.Pending;
                case "latest":
                    return BlockParameter.Latest;
                default:
                    return new BlockParameter(LongConverter.FromString(value));
            }
        }
    }
}
