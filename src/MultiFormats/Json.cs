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
using Newtonsoft.Json;

namespace MultiFormats
{
    public partial class MultiHash
    {
        /// <summary>
        ///   Conversion of a <see cref="MultiHash"/> to and from JSON.
        /// </summary>
        /// <remarks>
        ///   The JSON is just a single string value.
        /// </remarks>
        private sealed class Json : JsonConverter
        {
            public override bool CanConvert(Type objectType) { return true; }

            public override bool CanRead => true;
            public override bool CanWrite => true;

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var mh = value as MultiHash;
                writer.WriteValue(mh?.ToString());
            }

            public override object ReadJson(JsonReader reader,
                Type objectType,
                object existingValue,
                JsonSerializer serializer)
            {
                return !(reader.Value is string s) ? null : new MultiHash(s);
            }
        }
    }
}
