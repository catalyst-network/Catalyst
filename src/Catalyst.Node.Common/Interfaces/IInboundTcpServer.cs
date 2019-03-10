using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Catalyst.Node.Common.Interfaces {
    public interface IInboundTcpServer : IDisposable
    {
        /// <summary>
        ///     Call this in a try statement with a finally catch calling StopAsync/Dispose
        /// </summary>
        /// <returns></returns>
        Task StartAsync();
        Task StartAsync(X509Certificate x509Certificate);
        Task StopAsync();
        void Dispose();
    }
}