// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

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
