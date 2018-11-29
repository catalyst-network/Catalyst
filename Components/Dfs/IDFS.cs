using System.Threading.Tasks;

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
