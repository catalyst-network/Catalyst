using Catalyst.Node.Common.Interfaces;
using Ipfs.CoreApi;
using Ipfs.Engine;
using PeerTalk;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class InjectablePassphraseStringIpfsEngine : IpfsEngine
    {
        public InjectablePassphraseStringIpfsEngine(string passphrase)
            : base(passphrase.ToCharArray()) { }
    }
    
    public class IpfsConnector : IIpfsConnector
    {
        public IpfsConnector(IService service)
        {
            Service = service;
        }

        public IService Service { get; }
        public IBitswapApi Bitswap { get; }
        public IBlockApi Block { get; }
        public IBootstrapApi Bootstrap { get; }
        public IConfigApi Config { get; }
        public IDagApi Dag { get; }
        public IDhtApi Dht { get; }
        public IDnsApi Dns { get; }
        public IFileSystemApi FileSystem { get; }
        public IGenericApi Generic { get; }
        public IKeyApi Key { get; }
        public INameApi Name { get; }
        public IObjectApi Object { get; }
        public IPinApi Pin { get; }
        public IPubSubApi PubSub { get; }
        public IStatsApi Stats { get; }
        public ISwarmApi Swarm { get; }
    }
}