using System.IO;
using System.Threading.Tasks;

namespace ADL.Node.Core.Modules.Peer
{
    public interface IPeer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sslSettings"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task StartPeer(ISslSettings sslSettings, string options);

        bool StopPeer();
    }
}
