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
        public IService Service { get; }

        public IpfsConnector(IService service)
        {
            Service = service;
        }
    }
}