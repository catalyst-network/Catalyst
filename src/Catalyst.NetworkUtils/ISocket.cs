using System;
using System.Net;
using System.Threading.Tasks;

namespace Catalyst.NetworkUtils
{
    public interface ISocket : IDisposable
    {
        EndPoint LocalEndPoint { get; }
        public Task ConnectAsync(string host, int port);
        
    }
}
