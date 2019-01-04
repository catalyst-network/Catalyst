using System.Net;
using ADL.FileSystem;

namespace ADL.Node
{
    /// <summary>
    /// Default options for node
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
        public uint WalletRpcPort { get; set; } = 0;
        public string PublicKey { get; set; } = null;
        public string Network { get; set; } = "devnet"; //@TODO hardcoded network
        public string PayoutAddress{ get; set; } = null;
        public IPAddress WalletRpcIp { get; set; } = null;
        public IPAddress Host { get; set; } = IPAddress.Parse("127.0.0.1"); //@TODO hardcoded network
        public string DataDir { get; set; } = Fs.GetUserHomeDir()+"/.Atlas"; //@TODO hardcoded network
    }
}
