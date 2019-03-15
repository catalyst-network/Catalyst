using System;
using System.Net;

namespace Catalyst.Node.Common.Interfaces
{
    public interface ICLIRPCServerSettings
    {
        int Port { get; }
        IPAddress BindAddress { get; }
        string CertFileName { get; }
        string SslCertPassword { get; }
    }
}