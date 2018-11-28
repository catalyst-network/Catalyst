using System.IO;
using ADL.Cryptography.SSL;
using System.Threading.Tasks;

namespace ADL.P2P
{
    public interface IP2P
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        Task StartServer(IP2PSettings p2PSettings, ISslSettings sslSettings, DirectoryInfo dataDir);
    }
}
