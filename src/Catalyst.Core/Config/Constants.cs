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
using System.Linq;
using Catalyst.Abstractions.Enumerator;
using Catalyst.Abstractions.Types;
using Catalyst.Core.Util;
using Multiformats.Base;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;
using Nethereum.RLP;

namespace Catalyst.Core.Config
{
    public static class Constants
    {
        // <summary> Folder with config files </summary>
        public static string ConfigSubFolder => "Config";
        
        // <summary> Folder with modules for Catalyst.Node </summary>
        public static string ModulesSubFolder => "Modules";
        
        // <summary> Config file with Catalyst.Node component registrations for autofac </summary>
        public static string ComponentsJsonConfigFile => "components.json";
        
        // <summary> Serilog configuration file </summary>
        public static string SerilogJsonConfigFile => "serilog.json";
        
        // <summary> Search pattern for Json files </summary>
        public static string JsonFilePattern => "{0}.json";
        
        // <summary> Default Catalyst data directory </summary>
        public static string CatalystDataDir => ".catalyst";
        
        // <summary> Default dfs data directory inside the Catalyst data directory </summary>
        public static string DfsDataSubDir => "dfs";

        // <summary> Default keystore data directory inside the Catalyst data directory </summary>
        public static string KeyStoreDataSubDir => "keystore";
        
        // <summary> Config file with Catalyst.Cli component registrations for autofac </summary>
        public static string ShellComponentsJsonConfigFile => "shell.components.json";
        
        // <summary> Config file with nodes for use in rpc client </summary>
        public static string ShellNodesConfigFile => "nodes.json";
        
        // <summary> Shell configuration file </summary>
        public static string ShellConfigFile => "shell.config.json";
        
        // <summary> Registration of message handlers for autofac </summary>
        public static string MessageHandlersConfigFile => "messageHandlers.json";

        /// <summary>The allowed RPC node operators default XML configuration.</summary>
        public static string RpcAuthenticationCredentialsFile => "AuthCredentials.xml";

        /// <summary>The expiry minutes of initialization </summary>
        public static int FileTransferExpirySeconds => 60;

        /// <summary>The chunk size in bytes </summary>
        public static int FileTransferChunkSize => 200000;
        
        /// <summary>The maximum chunk retry count </summary>
        public static int FileTransferMaxChunkRetryCount => 3;

        /// <summary>The maximum chunk read tries </summary>
        public static int FileTransferMaxChunkReadTries => 30;

        /// <summary>
        ///     https://www.urbandictionary.com/define.php?term=angry+pirate
        /// </summary>
        public static int AngryPirate => 5;
        
        /// <summary> How many peers node discovers before saving for burn in value </summary>
        public static int PeerDiscoveryBurnIn => 25;
        
        /// <summary> EdDSA Curve  type </summary>
        public static string KeyChainDefaultKeyType => "ed25519";

        /// <summary> Hashing algorithm </summary>
        public static IMultihashAlgorithm HashAlgorithm { get; } = new BLAKE2B_256();

        /// <summary> Hashing algorithm type </summary>
        public static HashType HashAlgorithmType => HashAlgorithm.Code;

        public static MultibaseEncoding EncodingAlgorithm => MultibaseEncoding.Base58Btc;

        public static int BaseReputationChange => 1;
        
        /// <summary> Number of random peers to provide when processing a GetNeighbourRequest</summary>
        public static int NumberOfRandomPeers => 5;

        /// <summary> The empty data hash </summary>
        public static readonly byte[] EmptyDataHash = Multihash.Encode<BLAKE2B_8>(ByteUtil.EmptyByteArray);
        
        /// <summary> The empty trie hash </summary>
        public static readonly byte[] EmptyTrieHash = Multihash.Encode<BLAKE2B_8>(RLP.EncodeElement(ByteUtil.EmptyByteArray));

        /// <summary> This merkle tree first n bits. In this case 5 </summary>
        public static int MerkleTreeFirstStandardBits => 5;

        /// <summary> This is the standard size for salt byte array </summary>
        public static int StandardSaltSize => 100;
        
        public static IEnumerable<string> AllModuleFiles =>
            Enumeration.GetAll<ModuleTypes>()
               .Select(m => Path.Combine(ModulesSubFolder, string.Format(JsonFilePattern, m.Name.ToLower())));

        public static string NetworkConfigFile(Protocol.Common.Network network, string overrideNetworkFile = null)
        {
            return overrideNetworkFile ?? string.Format(JsonFilePattern, network.ToString().ToLowerInvariant());
        }
    }
}
