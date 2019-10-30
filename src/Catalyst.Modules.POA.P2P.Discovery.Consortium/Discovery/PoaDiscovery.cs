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
using System.Threading.Tasks;
using Catalyst.Abstractions.FileSystem;
using Catalyst.Abstractions.P2P.Discovery;
using Catalyst.Core.Lib.P2P.Models;
using Catalyst.Core.Lib.P2P.Repository;
using Catalyst.Core.Lib.Util;
using Newtonsoft.Json;
using Serilog;

namespace Catalyst.Modules.POA.P2P.Discovery
{
    public sealed class PoaDiscovery : IPeerDiscovery
    {
        public static string PoaPeerFile => "poa.nodes.json";
        private readonly IPeerRepository _peerRepository;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public PoaDiscovery(IPeerRepository peerRepository, IFileSystem fileSystem, ILogger logger)
        {
            _peerRepository = peerRepository;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <summary>
        ///     @TODO get from container eventually 
        /// </summary>
        /// <returns></returns>
        private string CopyPoaFile()
        {
            var poaPeerFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", PoaPeerFile);
            var target = Path.Combine(_fileSystem.GetCatalystDataDir().ToString(), PoaPeerFile);
            if (File.Exists(target))
            {
                return target;
            }

            File.Copy(poaPeerFile, target, true);
            return target;
        }

        public Task DiscoveryAsync()
        {
            var copiedPath = CopyPoaFile();
            var poaPeers = JsonConvert.DeserializeObject<List<PoaPeer>>(File.ReadAllText(copiedPath));

            foreach (var peer in poaPeers.Select(poaPeer => new Peer
            {
                PeerId = poaPeer.ToPeerId()
            }))
            {
                _logger.Information(
                    $"Adding POA Peer: {peer.PeerId.IpAddress} Public Key: {peer.PeerId.PublicKey.KeyToString()}");

                if (!_peerRepository.Exists(peer.DocumentId))
                {
                    _peerRepository.Add(peer);
                }
            }

            return Task.CompletedTask;
        }
    }
}
