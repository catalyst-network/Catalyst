using System.Net;
using System.Threading.Tasks;

namespace Catalyst.NetworkUtils
{
    public class WebClientWrapper : IWebClient
    {
        private readonly WebClient _webClient;

        public WebClientWrapper()
        {
            _webClient = new WebClient();
        }
        public Task<string> DownloadStringTaskAsync(string address)
        {
            return _webClient.DownloadStringTaskAsync(address);
        }
    }
}
