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
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    internal sealed partial class UnixFsApi
    {
        /// <summary>
        ///   A Block service that only computes the block's hash.
        /// </summary>
        private sealed class HashOnlyBlockService : IBlockApi
        {
            public IPinApi PinApi { get; set; }

            public Task<Cid> PutAsync(byte[] data,
                string contentType = Cid.DefaultContentType,
                string multiHash = MultiHash.DefaultAlgorithmName,
                string encoding = MultiBase.DefaultAlgorithmName,
                bool pin = false,
                CancellationToken cancel = default)
            {
                var cid = new Cid
                {
                    ContentType = contentType,
                    Encoding = encoding,
                    Hash = MultiHash.ComputeHash(data, multiHash),
                    Version = (contentType == "dag-pb" && multiHash == "sha2-256") ? 0 : 1
                };
                return Task.FromResult(cid);
            }
            
            public Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default)
            {
                throw new NotImplementedException();
            }

            public Task<Cid> PutAsync(Stream data,
                string contentType = Cid.DefaultContentType,
                string multiHash = MultiHash.DefaultAlgorithmName,
                string encoding = MultiBase.DefaultAlgorithmName,
                bool pin = false,
                CancellationToken cancel = default)
            {
                throw new NotImplementedException();
            }

            public Task<Cid> RemoveAsync(Cid id,
                bool ignoreNonexistent = false,
                CancellationToken cancel = default)
            {
                throw new NotImplementedException();
            }

            public Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}
