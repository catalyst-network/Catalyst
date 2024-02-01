// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only
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

using System;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lib.P2P;
using MultiFormats;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Evm.Tracing.GethStyle.JavaScript;
using Nethermind.Int256;

namespace Nethermind.Serialization.Json;

public class CidJsonConverter : JsonConverter<Cid?>
{
    public override Cid? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var hash256 = ByteArrayConverter.Convert(ref reader);
        if (hash256 == null)
        {
            return null;
        }

        return new Cid
        {
            Version = 1,
            Encoding = "base32",
            ContentType = "dag-pb",
            Hash = new MultiHash("blake2b-256", hash256.ToBytes())
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        Cid cid,
        JsonSerializerOptions options)
    {
        if (cid == null)
        {
            writer.WriteNullValue();
            return;
        }
        else
        {
            writer.WriteRawValue(ToHash256(cid).Bytes.ToHexString(true));
        }
    }

    static Hash256 ToHash256(Cid value) { return value == null ? null : new Hash256(value.Hash.Digest); }
}
