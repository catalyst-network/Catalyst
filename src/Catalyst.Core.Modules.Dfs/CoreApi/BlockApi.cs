﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Catalyst.Abstractions.Dfs.CoreApi;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Lib.FileSystem;
using Common.Logging;
using Lib.P2P;
using MultiFormats;

namespace Catalyst.Core.Modules.Dfs.CoreApi
{
    [DataContract]
    class DataBlock : IDataBlock
    {
        [DataMember]
        public byte[] DataBytes { get; set; }

        public Stream DataStream { get { return new MemoryStream(DataBytes, false); } }

        [DataMember]
        public Cid Id { get; set; }

        [DataMember]
        public long Size { get; set; }
    }

    class BlockApi : IBlockApi
    {
        static ILog log = LogManager.GetLogger(typeof(BlockApi));

        static readonly DataBlock emptyDirectory = new DataBlock
        {
            DataBytes = ObjectApi.EmptyDirectory.ToArray(),
            Id = ObjectApi.EmptyDirectory.Id,
            Size = ObjectApi.EmptyDirectory.ToArray().Length
        };

        static DataBlock emptyNode = new DataBlock
        {
            DataBytes = ObjectApi.EmptyNode.ToArray(),
            Id = ObjectApi.EmptyNode.Id,
            Size = ObjectApi.EmptyNode.ToArray().Length
        };

        FileStore<Cid, DataBlock> _store;

        private readonly IDhtApi _dhtApi;
        private readonly ISwarmApi _swarmApi;
        private readonly IBitswapApi _bitswapApi;
        private readonly IFileSystem _fileSystem;
        private readonly BlockOptions _blockOptions;

        public IPinApi PinApi { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bitswapApi"></param>
        /// <param name="dhtApi"></param>
        /// <param name="swarmApi"></param>
        /// <param name="pinApi"></param>
        public BlockApi(IBitswapApi bitswapApi, IDhtApi dhtApi, ISwarmApi swarmApi, IFileSystem fileSystem, BlockOptions blockOptions)
        {
            _bitswapApi = bitswapApi;
            _dhtApi = dhtApi;
            _swarmApi = swarmApi;
            _fileSystem = fileSystem;
            _blockOptions = blockOptions;
        }

        public FileStore<Cid, DataBlock> Store
        {
            get
            {
                if (_store != null)
                {
                    return _store;
                }
                
                var folder = Path.Combine(_fileSystem.GetCatalystDataDir().FullName, "blocks");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                
                _store = new FileStore<Cid, DataBlock>
                {
                    Folder = folder,
                    NameToKey = (cid) => cid.Hash.ToBase32(),
                    KeyToName = (key) => new MultiHash(key.FromBase32()),
                    Serialize = async (stream, cid, block, cancel) =>
                    {
                        await stream.WriteAsync(block.DataBytes, 0, block.DataBytes.Length, cancel)
                           .ConfigureAwait(false);
                    },
                    Deserialize = async (stream, cid, cancel) =>
                    {
                        var block = new DataBlock
                        {
                            Id = cid,
                            Size = stream.Length
                        };
                        block.DataBytes = new byte[block.Size];
                        for (int i = 0, n; i < block.Size; i += n)
                        {
                            n = await stream.ReadAsync(block.DataBytes, i, (int) block.Size - i, cancel)
                               .ConfigureAwait(false);
                        }

                        return block;
                    }
                };

                return _store;
            }
        }

        public async Task<IDataBlock> GetAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            // Hack for empty object and empty directory object
            if (id == emptyDirectory.Id)
            {
                return emptyDirectory;
            }
            
            if (id == emptyNode.Id)
            {
                return emptyNode;
            }

            // If identity hash, then CID has the content.
            if (id.Hash.IsIdentityHash)
            {
                return new DataBlock
                {
                    DataBytes = id.Hash.Digest,
                    Id = id,
                    Size = id.Hash.Digest.Length
                };
            }

            // Check the local filesystem for the block.
            var block = await Store.TryGetAsync(id, cancel).ConfigureAwait(false);
            if (block != null)
            {
                return block;
            }

            // Query the network, via DHT, for peers that can provide the
            // content.  As a provider peer is found, it is connected to and
            // the bitswap want lists are exchanged.  Hopefully the provider will
            // then send the block to us via bitswap and the get task will finish.
            using (var queryCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel))
            {
                var bitswapGet = _bitswapApi.GetAsync(id, queryCancel.Token).ConfigureAwait(false);
                
                var providers = await _dhtApi.FindProvidersAsync(
                    id: id,
                    cancel: queryCancel.Token
                );

                var enumerable = providers as Peer[] ?? providers.ToArray();
                for (var index = 0; index < enumerable.ToArray().Length; index++)
                {
                    var peer = enumerable.ToArray()[index];
                    var __ = ProviderFoundAsync(peer, cancel: queryCancel.Token).ConfigureAwait(false);
                }

                var got = await bitswapGet;
                log.Debug("bitswap got the block");

                queryCancel.Cancel(false); // stop the network query.
                return got;
            }
        }

        async Task ProviderFoundAsync(Peer peer, CancellationToken cancel)
        {
            if (cancel.IsCancellationRequested)
            {
                return;
            }

            log.Debug($"Connecting to provider {peer.Id}");
            try
            {
                await _swarmApi.ConnectAsync(peer, cancel).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Warn($"Connection to provider {peer.Id} failed, {e.Message}");
            }
        }

        public async Task<Cid> PutAsync(byte[] data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default)
        {
            if (data.Length > _blockOptions.MaxBlockSize)
            {
                throw new ArgumentOutOfRangeException($"data.Length",
                    $@"Block length can not exceed {_blockOptions.MaxBlockSize}.");
            }

            // Small enough for an inline CID?
            if (_blockOptions.AllowInlineCid && data.Length <= _blockOptions.InlineCidLimit)
            {
                return new Cid
                {
                    ContentType = contentType,
                    Hash = MultiHash.ComputeHash(data, "identity")
                };
            }

            // CID V1 encoding defaulting to base32 which is not
            // the multibase default. 
            var cid = new Cid
            {
                ContentType = contentType,
                Hash = MultiHash.ComputeHash(data, multiHash)
            };
            
            if (encoding != "base58btc")
            {
                cid.Encoding = encoding;
            }

            var block = new DataBlock
            {
                DataBytes = data,
                Id = cid,
                Size = data.Length
            };
            
            if (await Store.ExistsAsync(cid, cancel).ConfigureAwait(false))
            {
                log.DebugFormat("Block '{0}' already present", cid);
            }
            else
            {
                await Store.PutAsync(cid, block, cancel).ConfigureAwait(false);
                await _dhtApi.ProvideAsync(cid, advertise: false, cancel: cancel).ConfigureAwait(false);

                log.DebugFormat("Added block '{0}'", cid);
            }

            // Inform the Bitswap service.
            _bitswapApi.FoundBlock(block);

            // To pin or not.
            if (pin)
            {
                await PinApi.AddAsync(cid, recursive: false, cancel: cancel).ConfigureAwait(false);
            }
            else
            {
                await PinApi.RemoveAsync(cid, recursive: false, cancel: cancel).ConfigureAwait(false);
            }

            return cid;
        }

        public async Task<Cid> PutAsync(Stream data,
            string contentType = Cid.DefaultContentType,
            string multiHash = MultiHash.DefaultAlgorithmName,
            string encoding = MultiBase.DefaultAlgorithmName,
            bool pin = false,
            CancellationToken cancel = default(CancellationToken))
        {
            using (var ms = new MemoryStream())
            {
                await data.CopyToAsync(ms, cancel).ConfigureAwait(false);
                return await PutAsync(ms.ToArray(), contentType, multiHash, encoding, pin, cancel)
                   .ConfigureAwait(false);
            }
        }

        public async Task<Cid> RemoveAsync(Cid id,
            bool ignoreNonexistent = false,
            CancellationToken cancel = default(CancellationToken))
        {
            if (id.Hash.IsIdentityHash)
            {
                return id;
            }

            if (await Store.ExistsAsync(id, cancel).ConfigureAwait(false))
            {
                await Store.RemoveAsync(id, cancel).ConfigureAwait(false);
                await PinApi.RemoveAsync(id, recursive: false, cancel: cancel).ConfigureAwait(false);
                return id;
            }

            if (ignoreNonexistent)
            {
                return null;
            }
            
            throw new KeyNotFoundException($"Block '{id.Encode()}' does not exist.");
        }

        public async Task<IDataBlock> StatAsync(Cid id, CancellationToken cancel = default(CancellationToken))
        {
            if (id.Hash.IsIdentityHash)
            {
                return await GetAsync(id, cancel).ConfigureAwait(false);
            }

            IDataBlock block = null;
            var length = await Store.LengthAsync(id, cancel).ConfigureAwait(false);
            if (length.HasValue)
            {
                block = new DataBlock
                {
                    Id = id,
                    Size = length.Value
                };
            }

            return block;
        }
    }
}
