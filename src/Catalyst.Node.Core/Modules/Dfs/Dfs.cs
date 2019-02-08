using System;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class Dfs : IDisposable, IDfs
    {
        private readonly IIpfs _ipfs;

        /// <summary>
        /// </summary>
        /// <param name="ipfs"></param>
        public Dfs(IIpfs ipfs)
        {
            _ipfs = ipfs;
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            _ipfs.DestroyIpfsClient();
        }

        /// <summary>
        /// </summary>
        /// <param name="ipfsVersionApi"></param>
        /// <param name="connectRetries"></param>
        public void Start(string ipfsVersionApi, int connectRetries)
        {
            _ipfs.CreateIpfsClient(ipfsVersionApi, connectRetries);
        }

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string AddFile(string filename)
        {
            return _ipfs.AddFile(filename);
        }

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public Task<string> ReadAllTextAsync(string filename)
        {
            return _ipfs.ReadAllTextAsync(filename);
        }
    }
}