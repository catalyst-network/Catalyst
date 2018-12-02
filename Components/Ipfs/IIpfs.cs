using System.Threading.Tasks;

namespace ADL.Ipfs
{
    public interface IIpfs
    {
        string AddFile(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}
