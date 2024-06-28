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

using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using MultiFormats.Registry;

namespace MultiFormats
{
    /// <summary>
    ///   Wraps other formats with a tiny bit of self-description.
    /// </summary>
    /// <remarks>
    ///   <b>MultiCodec</b> is a self-describing multiformat, it wraps other formats with a 
    ///   tiny bit of self-description. A multicodec identifier is both a varint and the code 
    ///   identifying the following data.
    ///   <para>
    ///   Adds the following extension methods to <see cref="Stream"/>
    ///    <list type="bullet">
    ///      <item><description>ReadMultiCodec</description></item>
    ///      <item><description><see cref="WriteMultiCodec"/></description></item>
    ///    </list>
    ///   </para>
    /// </remarks>
    /// <seealso href="https://github.com/multiformats/multicodec"/>
    /// <seealso cref="Registry.Codec"/>
    public static class MultiCodec
    {
        /// <summary>
        ///   Reads a <see cref="Codec"/> from the <see cref="Stream"/>. 
        /// </summary>
        /// <param name="stream">
        ///   A multicodec encoded <see cref="Stream"/>.
        /// </param>
        /// <returns>The codec.</returns>
        /// <remarks>
        ///   If the <b>code</b> does not exist, a new <see cref="Codec"/> is
        ///   registered with the <see cref="Codec.Name"/> "codec-x"; where
        ///   'x' is the code's decimal represention.
        /// </remarks>
        public static Codec ReadMultiCodec(this Stream stream)
        {
            var code = stream.ReadVarint32();
            Codec.Codes.TryGetValue(code, out var codec);
            if (codec == null) codec = Codec.Register($"codec-{code}", code);

            return codec;
        }

        /// <summary>
        ///   Reads a <see cref="Codec"/> from the <see cref="Google.Protobuf.CodedInputStream"/>. 
        /// </summary>
        /// <param name="stream">
        ///   A multicodec encoded <see cref="Google.Protobuf.CodedInputStream"/>.
        /// </param>
        /// <returns>The codec.</returns>
        /// <remarks>
        ///   If the <b>code</b> does not exist, a new <see cref="Codec"/> is
        ///   registered with the <see cref="Codec.Name"/> "codec-x"; where
        ///   'x' is the code's decimal represention.
        /// </remarks>
        public static Codec ReadMultiCodec(this CodedInputStream stream)
        {
            var code = stream.ReadInt32();
            Codec.Codes.TryGetValue(code, out var codec);
            if (codec == null) codec = Codec.Register($"codec-{code}", code);

            return codec;
        }

        /// <summary>
        ///   Writes a <see cref="Codec"/> to the <see cref="Stream"/>. 
        /// </summary>
        /// <param name="stream">
        ///   A multicodec encoded <see cref="Stream"/>.
        /// </param>
        /// <param name="name">
        ///   The <see cref="Codec.Name"/>.
        /// </param>
        /// <remarks>
        ///   Writes the <see cref="Varint"/> of the <see cref="Codec.Code"/> to
        ///   the <paramref name="stream"/>.
        /// </remarks>
        /// <exception cref="KeyNotFoundException">
        ///   When <paramref name="name"/> is not registered.
        /// </exception>
        public static void WriteMultiCodec(this Stream stream, string name)
        {
            Codec.Names.TryGetValue(name, out var codec);
            if (codec == null) throw new KeyNotFoundException($"Codec '{name}' is not registered.");

            stream.WriteVarint(codec.Code);
        }
    }
}
