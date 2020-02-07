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

namespace Lib.P2P
{
    public sealed partial class Cid
    {
        /// <summary>
        ///   Conversion of a <see cref="Cid"/> to and from JSON.
        /// </summary>
        /// <remarks>
        ///   The JSON is just a single string value.
        /// </remarks>
        public class CidJsonConverter : JsonConverter
        {
            /// <inheritdoc />
            public override bool CanConvert(Type objectType) { return true; }

            /// <inheritdoc />
            public override bool CanRead => true;

            /// <inheritdoc />
            public override bool CanWrite => true;

            /// <inheritdoc />
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var cid = value as Cid;
                writer.WriteValue(cid?.Encode());
            }

            /// <inheritdoc />
            public override object ReadJson(JsonReader reader,
                Type objectType,
                object existingValue,
                JsonSerializer serializer)
            {
                return !(reader.Value is string s) ? null : Decode(s);
            }
        }
    }
}
