using System.IO;
using System.Linq;
using Catalyst.Abstractions.Hashing;
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
    public class ProtobufFormat : ILinkedDataFormat
    {
        private readonly IHashProvider _hashProvider;

        public ProtobufFormat()
        {
            _hashProvider = new HashProvider(HashingAlgorithm.GetAlgorithmMetadata("blake2b-256"));
        }

        /// <inheritdoc />
        public CBORObject Deserialise(byte[] data)
        {
            using (var ms = new MemoryStream(data, false))
            {
                var node = new DagNode(ms, _hashProvider);
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
            var node = new DagNode(data["data"].GetByteString(), _hashProvider, links);
            using (var ms = new MemoryStream())
            {
                node.Write(ms);
                return ms.ToArray();
            }
        }
    }
}
