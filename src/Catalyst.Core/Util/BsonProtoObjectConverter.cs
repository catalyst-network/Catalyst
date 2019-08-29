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
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Newtonsoft.Json.Linq;

namespace Catalyst.Core.Util
{
    public class ProtoBsonSerializer<T> : IBsonSerializer where T : IMessage, new()
    {
        public Type ValueType => typeof(T);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var parser = new JsonParser(JsonParser.Settings.Default);
            var buffer = context.Reader.ReadRawBsonDocument();
            var json = new RawBsonDocument(buffer).ToJson();
            var jObject = JObject.Parse(json);
            return parser.Parse<T>(jObject.ToString());
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            var formatter = new JsonFormatter(JsonFormatter.Settings.Default);
            string format = formatter.Format((T) value);
            var bsonDocument = BsonDocument.Parse(format);
            var raw = bsonDocument.ToBson();
            context.Writer.WriteRawBsonDocument(new ByteArrayBuffer(raw));
        }
    }
}
