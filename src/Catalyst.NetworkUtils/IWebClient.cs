using System.Threading.Tasks;

namespace Catalyst.NetworkUtils
{
    public interface IWebClient
    {
        Task<string> DownloadStringTaskAsync(string address);
    }
}
