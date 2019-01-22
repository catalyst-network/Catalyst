using System;
using System.Net;
using Catalyst.Helpers.FileSystem;

namespace Catalyst.Node
{
    /// <summary>
    ///     Default options for node
    /// </summary>
    public sealed class NodeOptions
    {
        public uint Env { get; set; } = 1;
        public bool Dfs { get; set; } = true;
        public bool Rpc { get; set; } = true;
        public bool Peer { get; set; } = true;
        public uint Platform { get; set; } = 0;
        public bool Gossip { get; set; } = true;
        public bool Daemon { get; set; } = false;
        public bool Ledger { get; set; } = false;
        public bool Mempool { get; set; } = true;
        public bool Contract { get; set; } = true;
        public bool Consensus { get; set; } = true;
        public string SeedServer { get; set; } = null;
        public uint WalletRpcPort { get; set; } = 0;
        public string PublicKey { get; set; } = null;
        public string Network { get; set; } = null;
        public string PayoutAddress { get; set; } = null;
        public IPAddress WalletRpcIp { get; set; } = null;
        public IPAddress Host { get; set; } = IPAddress.Parse("127.0.0.1"); //@TODO hardcoded network
        public string DataDir { get; set; } = Fs.GetUserHomeDir() + "/.Catalyst"; //@TODO hardcoded network
    }
}