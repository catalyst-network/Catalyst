using Microsoft.Extensions.Configuration;

namespace ADL.Rpc
{
    public interface IRpcSettings
    {
        int Port { get; set; }
        string BindAddress { get; set; }
        void Populate(IConfiguration section);
    }
}
