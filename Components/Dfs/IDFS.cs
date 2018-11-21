using System.Threading.Tasks;
using ADL.Node.Interfaces;

namespace ADL.DFS
{
    public interface IDFS
    {
        void Start(IDfsSettings settings);
        void Stop();
        string AddFile(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}
