using System.Threading.Tasks;

namespace Catalyst.Node.Common
{
    public interface IIpfs
    {
        string Name { get; }
        /// <summary>
        /// </summary>
        void DestroyIpfsClient();

        /// <summary>
        /// </summary>
        /// <param name="ipfsVersionApi"></param>
        /// <param name="connectRetries"></param>
        void CreateIpfsClient();

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        string AddFile(string filename);

        /// <summary>
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        Task<string> ReadAllTextAsync(string filename);
    }
}