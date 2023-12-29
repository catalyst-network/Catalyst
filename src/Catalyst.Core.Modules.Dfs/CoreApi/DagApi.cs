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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Core.Modules.Dfs.LinkedData;
using Lib.P2P;
using MultiFormats;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeterO.Cbor;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed class DagApi : IDagApi
    {
        private static readonly PODOptions PodOptions = new PODOptions
        (
            removeIsPrefix: false,
            useCamelCase: false
        );

        private readonly IBlockApi _blockApi;

        public DagApi(IBlockApi blockApi)
        {
            _blockApi = blockApi;
        }

        public async Task<JObject> GetAsync(Cid id,
            CancellationToken cancel = default)
        {
            var block = await _blockApi.GetAsync(id, cancel).ConfigureAwait(false);
            var format = GetDataFormat(id);
            var canonical = format.Deserialise(block.DataBytes);
            await using (var ms = new MemoryStream())
            using (var sr = new StreamReader(ms))
            using (var reader = new JsonTextReader(sr))
            {
                canonical.WriteJSONTo(ms);
                ms.Position = 0;
                return (JObject) JToken.ReadFrom(reader);
            }
        }

        public async Task<JToken> GetAsync(string path,
            CancellationToken cancel = default)
        {
            if (path.StartsWith("/ipfs/"))
            {
                path = path.Remove(0, 6);
            }

            var parts = path.Split('/').Where(p => p.Length > 0).ToArray();
            if (parts.Length == 0)
            {
                throw new ArgumentException($"Cannot resolve '{path}'.");
            }

            JToken token = await GetAsync(Cid.Decode(parts[0]), cancel).ConfigureAwait(false);
            foreach (var child in parts.Skip(1))
            {
                token = ((JObject) token)[child];
                if (token == null)
                {
                    throw new Exception($"Missing component '{child}'.");
                }
            }

            return token;
        }

        public async Task<T> GetAsync<T>(Cid id,
            CancellationToken cancel = default)
        {
            var block = await _blockApi.GetAsync(id, cancel).ConfigureAwait(false);
            var format = GetDataFormat(id);
            var canonical = format.Deserialise(block.DataBytes);

            // CBOR does not support serialisation to another Type
            // see https://github.com/peteroupc/CBOR/issues/12.
            // So, convert to JSON and use Newtonsoft to deserialise.
            return JObject.Parse(canonical.ToJSONString()).ToObject<T>();
        }

        public async Task<Cid> PutAsync(JObject data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default)
        {
            await using (var ms = new MemoryStream())
            await using (var sw = new StreamWriter(ms))
            using (var writer = new JsonTextWriter(sw))
            {
                await data.WriteToAsync(writer, cancel);
                writer.Flush();
                ms.Position = 0;
                var format = GetDataFormat(contentType);
                var block = format.Serialize(CBORObject.ReadJSON(ms));
                return await _blockApi.PutAsync(block, contentType, multiHash, encoding, pin, cancel)
                   .ConfigureAwait(false);
            }
        }

        public async Task<Cid> PutAsync(Stream data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default)
        {
            var format = GetDataFormat(contentType);
            var block = format.Serialize(CBORObject.Read(data));
            return await _blockApi.PutAsync(block, contentType, multiHash, encoding, pin, cancel)
               .ConfigureAwait(false);
        }

        public async Task<Cid> PutAsync(object data,
            string contentType = "dag-cbor",
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = true,
            CancellationToken cancel = default)
        {
            var format = GetDataFormat(contentType);
            var block = format.Serialize(CBORObject.FromObject(data, PodOptions));
            return await _blockApi.PutAsync(block, contentType, multiHash, encoding, pin, cancel)
               .ConfigureAwait(false);
        }

        private ILinkedDataFormat GetDataFormat(Cid id)
        {
            if (IpldRegistry.Formats.TryGetValue(id.ContentType, out var format))
            {
                return format;
            }

            throw new KeyNotFoundException($"Unknown IPLD format '{id.ContentType}'.");
        }

        private ILinkedDataFormat GetDataFormat(string contentType)
        {
            if (IpldRegistry.Formats.TryGetValue(contentType, out var format))
            {
                return format;
            }

            throw new KeyNotFoundException($"Unknown IPLD format '{contentType}'.");
        }
    }
}
