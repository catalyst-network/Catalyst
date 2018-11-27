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
        public bool P2P { get; set; } = true;
        public string Network { get; set; } = "devnet";
        public uint Platform { get; set; } = 0;
        public bool Daemon { get; set; } = false;
        public bool Gossip { get; set; } = true;
        public bool Contract { get; set; } = true;
        public bool Consensus { get; set; } = true;
        public uint WalletRpcPort { get; set; } = 0;
        public string PublicKey{ get; set; } = null;
        public string PayoutAddress{ get; set; } = null;
        public IPAddress WalletRpcIp { get; set; } = null;
        public IPAddress Host { get; set; } = IPAddress.Parse("127.0.0.1");
        public string DataDir { get; set; } = Fs.GetUserHomeDir()+"/.Atlas";
    }
}
