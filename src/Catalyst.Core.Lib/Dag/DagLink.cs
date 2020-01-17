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
using System.IO;
using Catalyst.Abstractions.Dfs;
using Google.Protobuf;
using Lib.P2P;

namespace Catalyst.Core.Lib.Dag
{
    /// <summary>
    ///   A link to another node in the IPFS Merkle DAG.
    /// </summary>
    public class DagLink : IMerkleLink
    {
        /// <summary>
        ///   Create a new instance of <see cref="DagLink"/> class.
        /// </summary>
        /// <param name="name">The name associated with the linked node.</param>
        /// <param name="id">The <see cref="Cid"/> of the linked node.</param>
        /// <param name="size">The serialised size (in bytes) of the linked node.</param>
        public DagLink(string name, Cid id, long size)
        {
            Name = name;
            Id = id;
            Size = size;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="DagLink"/> class from the
        ///   specified <see cref="IMerkleLink"/>.
        /// </summary>
        /// <param name="link">
        ///   Some type of a Merkle link.
        /// </param>
        public DagLink(IMerkleLink link)
        {
            Name = link.Name;
            Id = link.Id;
            Size = link.Size;
        }

        /// <summary>
        ///   Creates a new instance of the <see cref="DagLink"/> class from the
        ///   specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   A <see cref="Stream"/> containing the binary representation of the
        ///   <b>DagLink</b>.
        /// </param>
        public DagLink(Stream stream) { Read(stream); }

        /// <summary>
        ///   Creates a new instance of the <see cref="DagLink"/> class from the
        ///   specified <see cref="CodedInputStream"/>.
        /// </summary>
        /// <param name="stream">(
        ///   A <see cref="CodedInputStream"/> containing the binary representation of the
        ///   <b>DagLink</b>.
        /// </param>
        public DagLink(CodedInputStream stream) { Read(stream); }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public Cid Id { get; private set; }

        /// <inheritdoc />
        public long Size { get; private set; }

        /// <summary>
        ///   Writes the binary representation of the link to the specified <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="Stream"/> to write to.
        /// </param>
        public void Write(Stream stream)
        {
            using (var cos = new CodedOutputStream(stream, true))
            {
                Write(cos);
            }
        }

        /// <summary>
        ///   Writes the binary representation of the link to the specified <see cref="CodedOutputStream"/>.
        /// </summary>
        /// <param name="stream">
        ///   The <see cref="CodedOutputStream"/> to write to.
        /// </param>
        public void Write(CodedOutputStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            stream.WriteTag(1, WireFormat.WireType.LengthDelimited);
            Id.Write(stream);

            if (Name != null)
            {
                stream.WriteTag(2, WireFormat.WireType.LengthDelimited);
                stream.WriteString(Name);
            }

            stream.WriteTag(3, WireFormat.WireType.Varint);
            stream.WriteInt64(Size);
        }

        void Read(Stream stream)
        {
            using (var cis = new CodedInputStream(stream, true))
            {
                Read(cis);
            }
        }

        void Read(CodedInputStream stream)
        {
            while (!stream.IsAtEnd)
            {
                var tag = stream.ReadTag();
                switch (WireFormat.GetTagFieldNumber(tag))
                {
                    case 1:
                        Id = Cid.Read(stream);
                        break;
                    case 2:
                        Name = stream.ReadString();
                        break;
                    case 3:
                        Size = stream.ReadInt64();
                        break;
                    default:
                        throw new InvalidDataException("Unknown field number");
                }
            }
        }

        /// <summary>
        ///   Returns the IPFS binary representation as a byte array.
        /// </summary>
        /// <returns>
        ///   A byte array.
        /// </returns>
        public byte[] ToArray()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                return ms.ToArray();
            }
        }
    }
}
