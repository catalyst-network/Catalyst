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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Catalyst.Abstractions.Dfs;
using Google.Protobuf;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Lib.Dag
{
    /// <summary>
    ///     A node in the IPFS Merkle DAG.
    /// </summary>
    /// <remarks>
    ///     A <b>DagNode</b> has opaque <see cref="DagNode.DataBytes" />
    ///     and a set of navigable <see cref="DagNode.Links" />.
    /// </remarks>
    [DataContract]
    public sealed class DagNode : IDagNode, IEquatable<DagNode>
    {
        private Cid _id;
        private string _hashAlgorithm = MultiHash.DefaultAlgorithmName;
        private long? _size;

        /// <summary>
        ///     Create a new instance of a <see cref="DagNode" /> with the specified
        ///     <see cref="DagNode.DataBytes" /> and <see cref="DagNode.Links" />
        /// </summary>
        /// <param name="data">
        ///     The opaque data, can be <b>null</b>.
        /// </param>
        /// <param name="links">
        ///     The links to other nodes.
        /// </param>
        /// <param name="hashAlgorithm">
        ///     The name of the hashing algorithm to use; defaults to
        ///     <see cref="MultiHash.DefaultAlgorithmName" />.
        /// </param>
        public DagNode(byte[] data,
            IEnumerable<IMerkleLink> links = null,
            string hashAlgorithm = MultiHash.DefaultAlgorithmName)
        {
            DataBytes = data ?? new byte[0];
            Links = (links ?? new DagLink[0])
               .OrderBy(link => link.Name ?? "");
            _hashAlgorithm = hashAlgorithm;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="DagNode" /> class from the
        ///     specified <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        ///     A <see cref="Stream" /> containing the binary representation of the
        ///     <b>DagNode</b>.
        /// </param>
        public DagNode(Stream stream) { Read(stream); }

        /// <summary>
        ///     Creates a new instance of the <see cref="DagNode" /> class from the
        ///     specified <see cref="CodedInputStream" />.
        /// </summary>
        /// <param name="stream">
        ///     (
        ///     A <see cref="CodedInputStream" /> containing the binary representation of the
        ///     <b>DagNode</b>.
        /// </param>
        public DagNode(CodedInputStream stream) { Read(stream); }

        /// <inheritdoc />
        [DataMember]
        public IEnumerable<IMerkleLink> Links { get; private set; }

        /// <inheritdoc />
        [DataMember]
        public byte[] DataBytes { get; private set; }

        /// <inheritdoc />
        public Stream DataStream => new MemoryStream(DataBytes, false);

        /// <summary>
        ///     The serialised size in bytes of the node.
        /// </summary>
        [DataMember]
        public long Size
        {
            get
            {
                if (!_size.HasValue)
                {
                    ComputeSize();
                }

                return _size ?? throw new ArgumentNullException();
            }
        }

        /// <inheritdoc />
        [DataMember]
        public Cid Id
        {
            get
            {
                if (_id == null)
                {
                    ComputeHash();
                }

                return _id;
            }
            set
            {
                _id = value;
                if (_id != null)
                {
                    _hashAlgorithm = _id.Hash.Algorithm.Name;
                }
            }
        }

        /// <inheritdoc />
        public IMerkleLink ToLink(string name = "") { return new DagLink(name, Id, Size); }

        /// <summary>
        ///     Adds a link.
        /// </summary>
        /// <param name="link">
        ///     The link to add.
        /// </param>
        /// <returns>
        ///     A new <see cref="DagNode" /> with the existing and new
        ///     links.
        /// </returns>
        /// <remarks>
        ///     A <b>DagNode</b> is immutable.
        /// </remarks>
        public IDagNode AddLink(IMerkleLink link) { return AddLinks(new[] {link}); }

        /// <summary>
        ///     Adds a sequence of links.
        /// </summary>
        /// <param name="links">
        ///     The sequence of links to add.
        /// </param>
        /// <returns>
        ///     A new <see cref="DagNode" /> with the existing and new
        ///     links.
        /// </returns>
        /// <remarks>
        ///     A <b>DagNode</b> is immutable.
        /// </remarks>
        public IDagNode AddLinks(IEnumerable<IMerkleLink> links)
        {
            var all = Links.Union(links);
            return new DagNode(DataBytes, all, _hashAlgorithm);
        }

        /// <summary>
        ///     Removes a link.
        /// </summary>
        /// <param name="link">
        ///     The <see cref="IMerkleLink" /> to remove.
        /// </param>
        /// <returns>
        ///     A new <see cref="DagNode" /> with the <paramref name="link" />
        ///     removed.
        /// </returns>
        /// <remarks>
        ///     A <b>DagNode</b> is immutable.
        ///     <para>
        ///         No exception is raised if the <paramref name="link" /> does
        ///         not exist.
        ///     </para>
        /// </remarks>
        public DagNode RemoveLink(IMerkleLink link) { return RemoveLinks(new[] {link}); }

        /// <summary>
        ///     Remove a sequence of links.
        /// </summary>
        /// <param name="links">
        ///     The sequence of <see cref="IMerkleLink" /> to remove.
        /// </param>
        /// <returns>
        ///     A new <see cref="DagNode" /> with the <paramref name="links" />
        ///     removed.
        /// </returns>
        /// <remarks>
        ///     A <b>DagNode</b> is immutable.
        ///     <para>
        ///         No exception is raised if any of the <paramref name="links" /> do
        ///         not exist.
        ///     </para>
        /// </remarks>
        public DagNode RemoveLinks(IEnumerable<IMerkleLink> links)
        {
            var ignore = links.ToLookup(link => link.Id);
            var some = Links.Where(link => !ignore.Contains(link.Id));
            return new DagNode(DataBytes, some, _hashAlgorithm);
        }

        /// <summary>
        ///     Writes the binary representation of the node to the specified <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">
        ///     The <see cref="Stream" /> to write to.
        /// </param>
        public void Write(Stream stream)
        {
            using (var cos = new CodedOutputStream(stream, true))
            {
                Write(cos);
            }
        }

        /// <summary>
        ///     Writes the binary representation of the node to the specified <see cref="CodedOutputStream" />.
        /// </summary>
        /// <param name="stream">
        ///     The <see cref="CodedOutputStream" /> to write to.
        /// </param>
        public void Write(CodedOutputStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            foreach (var link in Links.Select(l => new DagLink(l)))
            {
                using (var linkStream = new MemoryStream())
                {
                    link.Write(linkStream);
                    var msg = linkStream.ToArray();
                    stream.WriteTag(2, WireFormat.WireType.LengthDelimited);
                    stream.WriteLength(msg.Length);
                    stream.WriteSomeBytes(msg);
                }
            }

            if (DataBytes.Length > 0)
            {
                stream.WriteTag(1, WireFormat.WireType.LengthDelimited);
                stream.WriteLength(DataBytes.Length);
                stream.WriteSomeBytes(DataBytes);
            }
        }

        private void Read(Stream stream)
        {
            using (var cis = new CodedInputStream(stream, true))
            {
                Read(cis);
            }
        }

        private void Read(CodedInputStream stream)
        {
            var links = new List<DagLink>();
            var done = false;

            while (!stream.IsAtEnd && !done)
            {
                var tag = stream.ReadTag();
                switch (WireFormat.GetTagFieldNumber(tag))
                {
                    case 1:
                        DataBytes = stream.ReadSomeBytes(stream.ReadLength());
                        done = true;
                        break;
                    case 2:
                        using (var ms = new MemoryStream(stream.ReadSomeBytes(stream.ReadLength())))
                        {
                            links.Add(new DagLink(ms));
                        }

                        break;
                    default:
                        throw new InvalidDataException("Unknown field number");
                }
            }

            if (DataBytes == null)
                DataBytes = new byte[0];
            Links = links.ToArray();
        }

        /// <summary>
        ///     Returns the IPFS binary representation as a byte array.
        /// </summary>
        /// <returns>
        ///     A byte array.
        /// </returns>
        public byte[] ToArray()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                return ms.ToArray();
            }
        }

        private void ComputeHash()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                _size = ms.Position;
                ms.Position = 0;
                _id = MultiHash.ComputeHash(ms, _hashAlgorithm);
            }
        }

        private void ComputeSize()
        {
            using (var ms = new MemoryStream())
            {
                Write(ms);
                _size = ms.Position;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            var that = obj as DagNode;
            return that != null && Id == that.Id;
        }

        /// <inheritdoc />
        public bool Equals(DagNode that) { return that != null && Id == that.Id; }

        /// <summary>
        ///     TODO
        /// </summary>
        public static bool operator ==(DagNode a, DagNode b)
        {
            return ReferenceEquals(a, b) || !ReferenceEquals(a, null) && (!ReferenceEquals(b, null) && a.Equals(b));
        }

        /// <summary>
        ///     TODO
        /// </summary>
        public static bool operator !=(DagNode a, DagNode b)
        {
            return !ReferenceEquals(a, b) && (ReferenceEquals(a, null) || (ReferenceEquals(b, null) || !a.Equals(b)));
        }
    }
}
