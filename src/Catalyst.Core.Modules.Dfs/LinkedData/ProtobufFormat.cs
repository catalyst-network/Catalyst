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

using System.IO;
using System.Linq;
using Catalyst.Core.Lib.Dag;
using Catalyst.Core.Modules.Hashing;
using Lib.P2P;
using MultiFormats.Registry;
using PeterO.Cbor;

namespace Catalyst.Core.Modules.Dfs.LinkedData
{
    /// <summary>
    ///   Linked data as a protobuf message.
    /// </summary>
    /// <remarks>
    ///   This is the original legacy format used by the IPFS <see cref="DagNode"/>. 
    /// </remarks>
    public sealed class ProtobufFormat : ILinkedDataFormat
    {
        public ProtobufFormat()
        {
            new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("keccak-256"));
        }

        /// <inheritdoc />
        public CBORObject Deserialise(byte[] data)
        {
            using (var ms = new MemoryStream(data, false))
            {
                var node = new DagNode(ms);
                var links = node.Links
                   .Select(link => CBORObject.NewMap()
                       .Add("Cid", CBORObject.NewMap()
                           .Add("/", link.Id.Encode())
                        )
                       .Add("Name", link.Name)
                       .Add("Size", link.Size))
                   .ToArray();
                var cbor = CBORObject.NewMap()
                   .Add("data", node.DataBytes)
                   .Add("links", links);
                return cbor;
            }
        }

        /// <inheritdoc />
        public byte[] Serialize(CBORObject data)
        {
            var links = data["links"].Values
               .Select(link => new DagLink(
                    link["Name"].AsString(),
                    Cid.Decode(link["Cid"]["/"].AsString()),
                    link["Size"].AsInt64()));
            var node = new DagNode(data["data"].GetByteString(), links);
            using (var ms = new MemoryStream())
            {
                node.Write(ms);
                return ms.ToArray();
            }
        }
    }
}
