using System.Threading.Tasks;

namespace Catalyst.Helpers.Ipfs
{
    public interface IIpfs
    {
        /// <summary>
        /// 
        /// </summary>
        void DestroyIpfsClient();
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipfsVersionApi"></param>
        /// <param name="connectRetries"></param>
        void CreateIpfsClient(string ipfsVersionApi, int connectRetries);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        string AddFile(string filename);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        Task<string> ReadAllTextAsync(string filename);
    }
}
