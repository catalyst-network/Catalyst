using System.IO;
using System.Threading.Tasks;

namespace ADL.Peer
{
    public interface IPeer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="p2PSettings"></param>
        /// <param name="sslSettings"></param>
        /// <param name="dataDir"></param>
        /// <returns></returns>
        Task StartServer(IPeerSettings p2PSettings, DirectoryInfo dataDir);
    }
}
