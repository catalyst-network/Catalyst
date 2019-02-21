using System;
using System.Threading.Tasks;
using Catalyst.Node.Common;
using Catalyst.Node.Common.Modules;

namespace Catalyst.Node.Core.Modules.Dfs
{
    public class IpfsDfs : IDisposable, IDfs
    {
        private readonly IIpfs _ipfs;
        private readonly ISettings _settings;

        /// <summary>
        /// </summary>
        /// <param name="ipfs"></param>
        /// <param name="settings"></param>
        public IpfsDfs(IIpfs ipfs, ISettings settings)
        {
            _ipfs = ipfs;
            _settings = settings;
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            _ipfs.DestroyIpfsClient();
        }

        public void Start()
        {
            _ipfs.CreateIpfsClient(_settings.IpfsVersionApi, _settings.ConnectRetries);
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

        public interface ISettings {
            ushort ConnectRetries { get; }
            string IpfsVersionApi { get; }
        }

        public class Settings : ISettings
        {
            protected internal Settings(ushort connectRetries, string apiVersion)
            {
                ConnectRetries = connectRetries;
                IpfsVersionApi = apiVersion;
            }
            public ushort ConnectRetries { get; set; }
            public string IpfsVersionApi { get; set; }
        }
    }
}