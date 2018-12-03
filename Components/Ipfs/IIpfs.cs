using System.Threading.Tasks;

namespace ADL.Ipfs
{
    public interface IIpfs
    {
        void DestroyIpfsClient();
        void CreateIpfsClient(string ipfsVersionApi, int connectRetries);
        string AddFile(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}
