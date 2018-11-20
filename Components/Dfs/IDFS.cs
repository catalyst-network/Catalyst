using System.Threading.Tasks;

namespace ADL.DFS
{
    public interface IDFS
    {
        string AddFile(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}
