using System.Threading.Tasks;

namespace ADL.Dfs
{
    public interface IDfs
    {
        void Start(IDfsSettings settings);
        void Stop();
        string AddFile(string filename);
        Task<string> ReadAllTextAsync(string filename);
    }
}
