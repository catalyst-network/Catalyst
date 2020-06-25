using System.Dynamic;

namespace Catalyst.NetworkUtils
{
    public class WebClientWrapperFactory : IWebClientFactory
    {
        public IWebClient Create()
        {
            return new WebClientWrapper();
        }
    }
}
