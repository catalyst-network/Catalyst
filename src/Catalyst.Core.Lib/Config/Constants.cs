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

using Multiformats.Base;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;

namespace Catalyst.Core.Lib.Config
{
    public static class Constants
    {
        // <summary> Folder with config files </summary>
        public static string ConfigSubFolder => "Config";

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
        
        // <summary> Config file with nodes for use in rpc client </summary>
        public static string ShellNodesConfigFile => "nodes.json";
        
        // <summary> RPC message handlers </summary>
        public static string RpcMessageHandlerConfigFile => "p2p.message.handlers.json";

        // <summary> P2P message handlers </summary>
        public static string P2PMessageHandlerConfigFile => "rpc.message.handlers.json";

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

        /// <summary> EdDSA Curve  type </summary>
        public static string KeyChainDefaultKeyType => "ed25519";

        /// <summary> Hashing algorithm </summary>
        public static IMultihashAlgorithm HashAlgorithm { get; } = new BLAKE2B_256();

        /// <summary> Hashing algorithm type </summary>
        public static HashType HashAlgorithmType => HashAlgorithm.Code;

        public static MultibaseEncoding EncodingAlgorithm => MultibaseEncoding.Base58Btc;

        /// <summary> Number of random peers to provide when processing a GetNeighbourRequest</summary>
        public static int NumberOfRandomPeers => 5;

        public static string NetworkConfigFile(Protocol.Common.Network network, string overrideNetworkFile = null)
        {
            return overrideNetworkFile ?? string.Format(JsonFilePattern, network.ToString().ToLowerInvariant());
        }
    }
}
