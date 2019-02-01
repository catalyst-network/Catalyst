using System;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;
using Catalyst.Node.Core.Helpers.Ipfs;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class Dfs : IDisposable, IDfs
    {
        private static Dfs Instance { get; set; }
        private static readonly object Mutex = new object();

        private readonly IIpfs _ipfs;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        private Dfs(IIpfs ipfs)
        {
            _ipfs = ipfs;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfs"></param>
        /// <returns></returns>
        public static Dfs GetInstance(IIpfs ipfs)
        {
            if (Instance == null)
                lock (Mutex)
                {
                    if (Instance == null) Instance = new Dfs(ipfs);
                }
            return Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfsVersionApi"></param>
        /// <param name="connectRetries"></param>
        public void Start(string ipfsVersionApi, int connectRetries)
        {
            _ipfs.CreateIpfsClient(ipfsVersionApi, connectRetries);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string AddFile(string filename)
        {
            return _ipfs.AddFile(filename);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Task<string> ReadAllTextAsync(string filename)
        {
            return _ipfs.ReadAllTextAsync(filename);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            _ipfs.DestroyIpfsClient();
        }
    }
}
