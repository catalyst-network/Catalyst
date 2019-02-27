using System.Collections.Generic;
using System.Net;

namespace Catalyst.Node.Common.P2P
{
    public interface IPeerSettings
    {
        string Network { get; }
        string PayoutAddress { get; }
        string PublicKey { get; }
        bool Announce { get; }
        IPEndPoint DnsServer { get; }
        IPEndPoint AnnounceServer { get; }
        bool MutualAuthentication { get; }
        bool AcceptInvalidCerts { get; }
        ushort MaxConnections { get; }
        int Port { get; }
        int Magic { get; }
        IPAddress BindAddress { get; }
        string PfxFileName { get; }
        List<string> KnownNodes { get; }
        List<string> SeedServers { get; }
        byte AddressVersion { get; }
        string SslCertPassword { get; }
    }
}