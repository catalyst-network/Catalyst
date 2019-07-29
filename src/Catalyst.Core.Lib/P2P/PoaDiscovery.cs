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

using Catalyst.Common.Interfaces.FileSystem;
using Catalyst.Common.Interfaces.P2P.Discovery;
using Catalyst.Common.Interfaces.Repository;
using Catalyst.Common.P2P;
using Nethereum.RLP;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Catalyst.Common.Util;
using Catalyst.Protocol.Common;

namespace Catalyst.Core.Lib.P2P
{
    public class PoaDiscovery : IPeerDiscovery
    {
        private readonly IPeerRepository _peerRepository;
        private readonly IFileSystem _fileSystem;
        private const string PoaPeerFile = "poaPeers.json";
        private readonly ILogger _logger;

        public PoaDiscovery(IPeerRepository peerRepository, IFileSystem fileSystem, ILogger logger)
        {
            _peerRepository = peerRepository;
            _fileSystem = fileSystem;
            _logger = logger;
            LoadPoaPeers();
        }

        /// <summary>
        /// Loads the POA peers from the JSON file <see cref="PoaPeerFile"/>
        /// </summary>
        private void LoadPoaPeers()
        {
            var copiedPath = CopyPoaFile();
            var poaPeers = JsonConvert.DeserializeObject<List<Peer>>(File.ReadAllText(copiedPath), new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = {new IpAddressConverter(), new IpEndPointConverter(), new ProtoObjectConverter<PeerId>()}
            });

            foreach (var poaPeer in poaPeers)
            {
                _logger.Information($"Adding POA Peer: {poaPeer.PeerIdentifier.Ip} Public Key: {poaPeer.PeerIdentifier.PublicKey.ToStringFromRLPDecoded()}");
                if (!_peerRepository.Exists(poaPeer.DocumentId))
                {
                    _peerRepository.Add(poaPeer);
                }
            }
        }

        private string CopyPoaFile()
        {
            var poaPeerFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", PoaPeerFile);
            var target = Path.Combine(_fileSystem.GetCatalystDataDir().ToString(), PoaPeerFile);
            File.Copy(poaPeerFile, target, true);
            return target;
        }

        public Task DiscoveryAsync() { return Task.CompletedTask; }
    }
}
