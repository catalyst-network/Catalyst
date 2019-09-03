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
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Util
{
    public class JsonProtoObjectConverter<T> : JsonConverter<IMessage<T>> where T : IMessage<T>, new()
    {
        public override void WriteJson(JsonWriter writer, IMessage<T> value, JsonSerializer serializer)
        {
            var formatter = new JsonFormatter(JsonFormatter.Settings.Default);
            writer.WriteRawValue(formatter.Format(value));
        }

        public override IMessage<T> ReadJson(JsonReader reader,
            Type objectType,
            IMessage<T> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var parser = new JsonParser(JsonParser.Settings.Default);

            // Load JObject from stream
            var jObject = JObject.Load(reader);
            return parser.Parse<T>(jObject.ToString());
        }
    }
}
