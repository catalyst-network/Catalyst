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

using MultiFormats;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private sealed class CidJsonConverter : JsonConverter<object>
        {
            public override object Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null
                ? default
                : Decode(reader.GetBytesFromBase64().ToString());

            public override void Write(
                Utf8JsonWriter writer,
                object value,
                JsonSerializerOptions options) =>
                writer.WriteStringValue((value as Cid)?.ToString());
        }
    }
}
